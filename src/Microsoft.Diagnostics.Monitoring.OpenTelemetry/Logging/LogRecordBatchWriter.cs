// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.OpenTelemetry.Resources;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Logging;

public abstract class LogRecordBatchWriter : IBatchWriter
{
    protected LogRecordBatchWriter()
    {
    }

    public virtual void BeginBatch(
        Resource resource)
    {
    }

    public virtual void EndBatch()
    {
    }

    public virtual void BeginInstrumentationScope(
        InstrumentationScope instrumentationScope)
    {
    }

    public virtual void EndInstrumentationScope()
    {
    }

    public virtual void WriteLogRecord(in LogRecord logRecord)
    {
    }
}
