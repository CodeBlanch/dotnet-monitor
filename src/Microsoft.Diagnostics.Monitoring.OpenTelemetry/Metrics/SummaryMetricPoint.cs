// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Metrics;

public readonly ref struct SummaryMetricPoint
{
    public readonly DateTime StartTimeUtc;

    public readonly DateTime EndTimeUtc;

    public readonly double Sum;

    public readonly int Count;

    public SummaryMetricPoint(
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        double sum,
        int count)
    {
        StartTimeUtc = startTimeUtc;
        EndTimeUtc = endTimeUtc;
        Sum = sum;
        Count = count;
    }
}
