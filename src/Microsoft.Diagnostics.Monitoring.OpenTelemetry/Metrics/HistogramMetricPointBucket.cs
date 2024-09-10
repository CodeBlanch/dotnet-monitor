// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Metrics;

public readonly struct HistogramMetricPointBucket
{
    public readonly double Value;

    public readonly int Count;

    public HistogramMetricPointBucket(
        double value,
        int count)
    {
        Value = value;
        Count = count;
    }
}

