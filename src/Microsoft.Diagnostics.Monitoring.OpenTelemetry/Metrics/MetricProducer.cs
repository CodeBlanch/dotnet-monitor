// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Metrics;

public abstract class MetricProducer
{
    protected MetricProducer()
    {
    }

    public abstract bool Produce(
        MetricWriter writer,
        CancellationToken cancellationToken);
}
