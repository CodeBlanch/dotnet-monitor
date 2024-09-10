// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.OpenTelemetry.Resources;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Logging;

public static class LogRecordExportProcessorFactory
{
    public static ILogRecordProcessor CreateBatchExportProcessor(
        ILoggerFactory loggerFactory,
        Resource resource,
        Exporter<LogRecordBatchWriter> exporter,
        BatchExportProcessorOptions options)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        return new LogRecordBatchExportProcessor(
            loggerFactory.CreateLogger<LogRecordBatchExportProcessor>(),
            resource,
            exporter,
            options);
    }
}
