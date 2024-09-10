// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.OpenTelemetry.Resources;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Tracing;

public static class SpanExportProcessorFactory
{
    public static ISpanProcessor CreateBatchExportProcessor(
        ILoggerFactory loggerFactory,
        Resource resource,
        Exporter<SpanBatchWriter> exporter,
        BatchExportProcessorOptions options)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        return new SpanBatchExportProcessor(
            loggerFactory.CreateLogger<SpanBatchExportProcessor>(),
            resource,
            exporter,
            options);
    }
}
