// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.OpenTelemetry.Resources;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Metrics;

internal sealed class PeriodicExportingMetricReader : MetricReader
{
    private readonly ILogger<PeriodicExportingMetricReader> _Logger;
    private readonly Resource _Resource;
    private readonly Exporter<MetricBatchWriter> _Exporter;
    private readonly MetricProducer[] _MetricProducers;
    private readonly Thread _ExporterThread;
    private readonly AutoResetEvent _ExportTrigger = new(false);
    private readonly ManualResetEvent _DataExportedTrigger = new(false);
    private readonly ManualResetEvent _ShutdownTrigger = new(false);
    private readonly int _ExportIntervalMilliseconds;
    private readonly int _ExportTimeoutMilliseconds;
    private TaskCompletionSource? _ShutdownTcs;
    private CancellationTokenSource? _ExportCts;
    private bool _Disposed;

    public PeriodicExportingMetricReader(
        ILogger<PeriodicExportingMetricReader> logger,
        Resource resource,
        Exporter<MetricBatchWriter> exporter,
        IEnumerable<MetricProducer>? metricProducers,
        PeriodicExportingMetricReaderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _Resource = resource ?? throw new ArgumentNullException(nameof(resource));
        _Exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
        _MetricProducers = (metricProducers ?? Array.Empty<MetricProducer>())
            .Where(p => p != null)
            .ToArray();
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
                if (!timedOut)
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

    private void ExporterProc(object? state)
    {
        var triggers = new WaitHandle[] { _ExportTrigger, _ShutdownTrigger };

        while (true)
        {
            int waitHandleIndex;
            try
            {
                waitHandleIndex = WaitHandle.WaitAny(triggers, _ExportIntervalMilliseconds);
            }
            catch (ObjectDisposedException)
            {
                // the exporter is somehow disposed before the worker thread could finish its job
                return;
            }

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

            Export(
                new MetricBatch(_Logger, _Resource, _MetricProducers),
                token);

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

            if (waitHandleIndex == 1)
            {
                _ShutdownTcs?.TrySetResult();
                return;
            }
        }
    }

    private void Export(in MetricBatch batch, CancellationToken cancellationToken)
    {
        try
        {
            var result = _Exporter.Export(in batch, cancellationToken);

            _Logger.LogInformation("Telemetry export completed with {Result} using '{ExporterType}' exporter", result, _Exporter.GetType());
        }
        catch (Exception ex)
        {
            _Logger.LogWarning(ex, "Exception thrown exporting telemetry using '{ExporterType}' exporter", _Exporter.GetType());
        }
    }
}
