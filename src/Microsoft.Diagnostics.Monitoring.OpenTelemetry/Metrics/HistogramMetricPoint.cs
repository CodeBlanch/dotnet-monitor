// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Metrics;

public readonly ref struct HistogramMetricPoint
{
    public readonly DateTime StartTimeUtc;

    public readonly DateTime EndTimeUtc;

    public readonly HistogramMetricPointFeatures Features;

    public readonly double Min;

    public readonly double Max;

    public readonly double Sum;

    public readonly int Count;

    public HistogramMetricPoint(
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        HistogramMetricPointFeatures features,
        double min,
        double max,
        double sum,
        int count)
    {
        StartTimeUtc = startTimeUtc;
        EndTimeUtc = endTimeUtc;
        Features = features;
        Min = min;
        Max = max;
        Sum = sum;
        Count = count;
    }
}

