// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Logging;

internal readonly ref struct BufferedLogRecordBatch : IBatch<LogRecordBatchWriter>
{
    private readonly BufferedTelemetryBatch<BufferedLogRecord> _BufferedBatch;

    public BufferedLogRecordBatch(
        BufferedTelemetryBatch<BufferedLogRecord> bufferedBatch)
    {
        Debug.Assert(bufferedBatch != null);

        _BufferedBatch = bufferedBatch;
    }

    public bool WriteTo(LogRecordBatchWriter writer, CancellationToken cancellationToken)
    {
        return _BufferedBatch.WriteTo(writer, WriteItemCallback, cancellationToken);
    }

    private static void WriteItemCallback(
        LogRecordBatchWriter writer,
        BufferedLogRecord bufferedLogRecord)
    {
        bufferedLogRecord.ToLogRecord(out var logRecord);

        writer.WriteLogRecord(in logRecord);
    }
}
