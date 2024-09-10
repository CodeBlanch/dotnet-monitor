// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Metrics;

public sealed class PeriodicExportingMetricReaderOptions
{
    internal const int DefaultMaxQueueSize = 2048;
    internal const int DefaultMaxExportBatchSize = 512;
    internal const int DefaultExportIntervalMilliseconds = 5000;
    internal const int DefaultExportTimeoutMilliseconds = 30000;

    /// <summary>
    /// Gets the delay interval (in milliseconds) between two consecutive exports. The default value is 5000.
    /// </summary>
    public int ExportIntervalMilliseconds { get; }

    /// <summary>
    /// Gets the timeout (in milliseconds) after which the export is cancelled. The default value is 30000.
    /// </summary>
    /// <remarks>Note: Set to <c>-1</c> to disable timeout.</remarks>
    public int ExportTimeoutMilliseconds { get; }

    public PeriodicExportingMetricReaderOptions(
        int exportIntervalMilliseconds,
        int exportTimeoutMilliseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(exportIntervalMilliseconds, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(exportTimeoutMilliseconds, -1);

        ExportIntervalMilliseconds = exportIntervalMilliseconds;
        ExportTimeoutMilliseconds = exportTimeoutMilliseconds;
    }
}
