// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.OpenTelemetry.Resources;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Metrics;

public abstract class MetricBatchWriter : MetricWriter, IBatchWriter
{
    protected MetricBatchWriter()
    {
    }

    public virtual void BeginBatch(
        Resource resource)
    {
    }

    public virtual void EndBatch()
    {
    }
}
