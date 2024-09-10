// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.OpenTelemetry.Resources;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry;

internal abstract class BatchExportProcessor<TBufferedTelemetry, TBatchWriter, TBufferedBatch> : Processor
    where TBufferedTelemetry : class, IBufferedTelemetry<TBufferedTelemetry>
    where TBatchWriter : IBatchWriter
    where TBufferedBatch : IBatch<TBatchWriter>, allows ref struct
{
    private readonly ILogger _Logger;
    private readonly Exporter<TBatchWriter> _Exporter;
    private readonly CircularBuffer<TBufferedTelemetry> _CircularBuffer;
    private readonly BufferedTelemetryBatch<TBufferedTelemetry> _BufferedBatch;
    private readonly Thread _ExporterThread;
    private readonly AutoResetEvent _ExportTrigger = new(false);
    private readonly ManualResetEvent _DataExportedTrigger = new(false);
    private readonly ManualResetEvent _ShutdownTrigger = new(false);
    private readonly int _MaxExportBatchSize;
    private readonly int _ExportIntervalMilliseconds;
    private readonly int _ExportTimeoutMilliseconds;
    private long _DroppedCount;
    private long _ShutdownDrainTarget = long.MaxValue;
    private TaskCompletionSource? _ShutdownTcs;
    private CancellationTokenSource? _ExportCts;
    private bool _Disposed;

    protected BatchExportProcessor(
        ILogger logger,
        Resource resource,
        Exporter<TBatchWriter> exporter,
        BatchExportProcessorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _Exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));

        _CircularBuffer = new(options.MaxQueueSize);
        _BufferedBatch = new BufferedTelemetryBatch<TBufferedTelemetry>(resource);
        _MaxExportBatchSize = Math.Min(options.MaxExportBatchSize, options.MaxQueueSize);
        _ExportIntervalMilliseconds = options.ExportIntervalMilliseconds;
        _ExportTimeoutMilliseconds = options.ExportTimeoutMilliseconds;

        _ExporterThread = new Thread(ExporterProc)
        {
            IsBackground = true,
            Name = $"OpenTelemetry-{GetType()}-{exporter.GetType()}",
        };
        _ExporterThread.Start();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        var tail = _CircularBuffer.RemovedCount;
        var head = _CircularBuffer.AddedCount;

        if (head == tail)
        {
            return Task.CompletedTask;
        }

        try
        {
            _ExportTrigger.Set();
        }
        catch (ObjectDisposedException)
        {
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource();
        var rwh = ThreadPool.RegisterWaitForSingleObject(
            waitObject: _DataExportedTrigger,
            callBack: (state, timedOut) =>
            {
                if (!timedOut && _CircularBuffer.RemovedCount >= head)
                {
                    tcs.TrySetResult();
                }
                else if (cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled();
                }
            },
            state: null,
            millisecondsTimeOutInterval: 1000,
            executeOnlyOnce: false);

        return tcs.Task
            .ContinueWith((_) => rwh.Unregister(waitObject: null), TaskScheduler.Default);
    }

    public override Task ShutdownAsync(CancellationToken cancellationToken)
    {
        if (_ShutdownTcs != null)
        {
            return _ShutdownTcs.Task.WaitAsync(cancellationToken);
        }

        Volatile.Write(ref _ShutdownDrainTarget, _CircularBuffer.AddedCount);

        _ShutdownTcs = new TaskCompletionSource();

        _ShutdownTrigger.Set();

        return _ShutdownTcs.Task.WaitAsync(cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (!_Disposed)
        {
            if (disposing)
            {
                _ExportTrigger.Dispose();
                _DataExportedTrigger.Dispose();
                _ShutdownTrigger.Dispose();
                _ExportCts?.Dispose();
                _Exporter.Dispose();
            }

            _Disposed = true;
        }

        base.Dispose(disposing);
    }

    protected void AddItemToBatch(TBufferedTelemetry item)
    {
        Debug.Assert(item != null);

        if (_CircularBuffer.TryAdd(item, maxSpinCount: 50000))
        {
            if (_CircularBuffer.Count >= _MaxExportBatchSize)
            {
                try
                {
                    _ExportTrigger.Set();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }
        else
        {
            // either the queue is full or exceeded the spin limit, drop the item on the floor
            Interlocked.Increment(ref _DroppedCount);
        }
    }

    protected abstract void CreateBatch(
        BufferedTelemetryBatch<TBufferedTelemetry> bufferedBatch,
        out TBufferedBatch batch);

    private void ExporterProc(object? state)
    {
        var triggers = new WaitHandle[] { _ExportTrigger, _ShutdownTrigger };

        while (true)
        {
            // only wait when the queue doesn't have enough items, otherwise keep busy and send data continuously
            if (_CircularBuffer.Count < _MaxExportBatchSize)
            {
                try
                {
                    WaitHandle.WaitAny(triggers, _ExportIntervalMilliseconds);
                }
                catch (ObjectDisposedException)
                {
                    // the exporter is somehow disposed before the worker thread could finish its job
                    return;
                }
            }

            var targetCount = _CircularBuffer.RemovedCount + Math.Min(_MaxExportBatchSize, _CircularBuffer.Count);
            if (targetCount > 0)
            {
                while (_CircularBuffer.RemovedCount < targetCount)
                {
                    var item = _CircularBuffer.Read();

                    _BufferedBatch.Add(item);
                }

                try
                {
                    Export();
                }
                finally
                {
                    _BufferedBatch.Reset();
                }
            }

            try
            {
                _DataExportedTrigger.Set();
                _DataExportedTrigger.Reset();
            }
            catch (ObjectDisposedException)
            {
                // the exporter is somehow disposed before the worker thread could finish its job
                return;
            }

            if (_CircularBuffer.RemovedCount >= Volatile.Read(ref _ShutdownDrainTarget))
            {
                _ShutdownTcs?.TrySetResult();

                if (_DroppedCount > 0)
                {
                    _Logger.LogWarning(
                        "Batch export processor using '{ExporterType}' exporter dropped {DroppedItemCount} item(s) due to batch being full.",
                        _Exporter.GetType(),
                        _DroppedCount);
                }
                return;
            }
        }
    }

    private void Export()
    {
        try
        {
            CancellationToken token;
            if (_ExportTimeoutMilliseconds < 0)
            {
                token = CancellationToken.None;
            }
            else
            {
                if (_ExportCts == null || !_ExportCts.TryReset())
                {
                    var oldCts = _ExportCts;
                    _ExportCts = new CancellationTokenSource(_ExportTimeoutMilliseconds);
                    oldCts?.Dispose();
                }
                token = _ExportCts.Token;
            }

            CreateBatch(_BufferedBatch, out var batch);

            var result = _Exporter.Export(in batch, token);

            _Logger.LogInformation("Telemetry export completed with {Result} using '{ExporterType}' exporter", result, _Exporter.GetType());
        }
        catch (Exception ex)
        {
            _Logger.LogWarning(ex, "Exception thrown exporting telemetry using '{ExporterType}' exporter", _Exporter.GetType());
        }
    }
}
