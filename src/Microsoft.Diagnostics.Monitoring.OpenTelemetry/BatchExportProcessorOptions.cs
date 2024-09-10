// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry;

public sealed class BatchExportProcessorOptions
{
    internal const int DefaultMaxQueueSize = 2048;
    internal const int DefaultMaxExportBatchSize = 512;
    internal const int DefaultExportIntervalMilliseconds = 5000;
    internal const int DefaultExportTimeoutMilliseconds = 30000;

    /// <summary>
    /// Gets the maximum queue size. The queue drops the data if the maximum size is reached. The default value is 2048.
    /// </summary>
    public int MaxQueueSize { get; }

    /// <summary>
    /// Gets the maximum batch size of every export. It must be smaller or equal to MaxQueueLength. The default value is 512.
    /// </summary>
    public int MaxExportBatchSize { get; }

    /// <summary>
    /// Gets the delay interval (in milliseconds) between two consecutive exports. The default value is 5000.
    /// </summary>
    public int ExportIntervalMilliseconds { get; }

    /// <summary>
    /// Gets the timeout (in milliseconds) after which the export is cancelled. The default value is 30000.
    /// </summary>
    /// <remarks>Note: Set to <c>-1</c> to disable timeout.</remarks>
    public int ExportTimeoutMilliseconds { get; }

    public BatchExportProcessorOptions(
        int maxQueueSize,
        int maxExportBatchSize,
        int exportIntervalMilliseconds,
        int exportTimeoutMilliseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxQueueSize, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxExportBatchSize, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(exportIntervalMilliseconds, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(exportTimeoutMilliseconds, -1);

        if (maxExportBatchSize > maxQueueSize)
        {
            throw new ArgumentException($"The value supplied for {nameof(maxExportBatchSize)} must be less than or equal to the value supplied for {maxQueueSize}");
        }

        MaxQueueSize = maxQueueSize;
        MaxExportBatchSize = maxExportBatchSize;
        ExportIntervalMilliseconds = exportIntervalMilliseconds;
        ExportTimeoutMilliseconds = exportTimeoutMilliseconds;
    }
}
