// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.OpenTelemetry.Resources;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Tracing;

internal sealed class SpanBatchExportProcessor : BatchExportProcessor<BufferedSpan, SpanBatchWriter, BufferedSpanBatch>, ISpanProcessor
{
    public SpanBatchExportProcessor(
        ILogger<SpanBatchExportProcessor> logger,
        Resource resource,
        Exporter<SpanBatchWriter> exporter,
        BatchExportProcessorOptions options)
        : base(logger, resource, exporter, options)
    {
    }

    public void ProcessEndedSpan(in Span span)
    {
        var bufferedItem = new BufferedSpan(in span);

        AddItemToBatch(bufferedItem);
    }

    protected override void CreateBatch(
        BufferedTelemetryBatch<BufferedSpan> bufferedBatch,
        out BufferedSpanBatch batch)
    {
        batch = new(bufferedBatch);
    }
}
