// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Metrics;

public sealed class Metric
{
    public MetricType MetricType { get; }

    public string Name { get; }

    public string? Description { get; init; }

    public string? Unit { get; init; }

    public AggregationTemporality AggregationTemporality { get; }

    public bool IsSumNonMonotonic => ((byte)MetricType & 0x80) == 1;

    public Metric(MetricType metricType, string name, AggregationTemporality aggregationTemporality)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        MetricType = metricType;
        Name = name;
        AggregationTemporality = aggregationTemporality;

        if (IsSumNonMonotonic
            && AggregationTemporality == AggregationTemporality.Delta)
        {
            AggregationTemporality = AggregationTemporality.Cumulative;
        }
    }
}
