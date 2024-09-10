// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Metrics;

public abstract class MetricWriter
{
    protected MetricWriter()
    {
    }

    public virtual void BeginInstrumentationScope(
        InstrumentationScope instrumentationScope)
    {
    }

    public virtual void EndInstrumentationScope()
    {
    }

    public virtual void BeginMetric(
        Metric metric)
    {
    }

    public virtual void EndMetric()
    {
    }

    public virtual void WriteNumberMetricPoint(
        in NumberMetricPoint numberMetricPoint,
        ReadOnlySpan<KeyValuePair<string, object?>> attributes)
    {
    }

    public virtual void WriteHistogramMetricPoint(
        in HistogramMetricPoint histogramMetricPoint,
        ReadOnlySpan<HistogramMetricPointBucket> buckets,
        ReadOnlySpan<KeyValuePair<string, object?>> attributes)
    {
    }

    public virtual void WriteSummaryMetricPoint(
        in SummaryMetricPoint summaryMetricPoint,
        ReadOnlySpan<SummaryMetricPointQuantile> quantiles,
        ReadOnlySpan<KeyValuePair<string, object?>> attributes)
    {
    }
}
