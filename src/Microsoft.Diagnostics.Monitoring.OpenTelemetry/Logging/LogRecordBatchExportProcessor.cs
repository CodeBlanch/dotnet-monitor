// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.OpenTelemetry.Resources;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Logging;

internal sealed class LogRecordBatchExportProcessor : BatchExportProcessor<BufferedLogRecord, LogRecordBatchWriter, BufferedLogRecordBatch>, ILogRecordProcessor
{
    public LogRecordBatchExportProcessor(
        ILogger<LogRecordBatchExportProcessor> logger,
        Resource resource,
        Exporter<LogRecordBatchWriter> exporter,
        BatchExportProcessorOptions options)
        : base(logger, resource, exporter, options)
    {
    }

    public void ProcessEmittedLogRecord(in LogRecord logRecord)
    {
        var bufferedItem = new BufferedLogRecord(in logRecord);

        AddItemToBatch(bufferedItem);
    }

    protected override void CreateBatch(
        BufferedTelemetryBatch<BufferedLogRecord> bufferedBatch,
        out BufferedLogRecordBatch batch)
    {
        batch = new(bufferedBatch);
    }
}
