// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.OpenTelemetry.Resources;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Tracing;

public abstract class SpanBatchWriter : IBatchWriter
{
    protected SpanBatchWriter()
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

    public virtual void WriteSpan(in Span span)
    {
    }
}
