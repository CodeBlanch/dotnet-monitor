// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Tracing;

internal readonly ref struct BufferedSpanBatch : IBatch<SpanBatchWriter>
{
    private readonly BufferedTelemetryBatch<BufferedSpan> _BufferedBatch;

    public BufferedSpanBatch(
        BufferedTelemetryBatch<BufferedSpan> bufferedBatch)
    {
        Debug.Assert(bufferedBatch != null);

        _BufferedBatch = bufferedBatch;
    }

    public bool WriteTo(SpanBatchWriter writer, CancellationToken cancellationToken)
    {
        return _BufferedBatch.WriteTo(writer, WriteItemCallback, cancellationToken);
    }

    private static void WriteItemCallback(
        SpanBatchWriter writer,
        BufferedSpan bufferedSpan)
    {
        bufferedSpan.ToSpan(out var span);

        writer.WriteSpan(in span);
    }
}
