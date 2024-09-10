// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Metrics;

[StructLayout(LayoutKind.Explicit)]
public readonly struct NumberMetricPoint
{
    [FieldOffset(0)]
    public readonly double ValueAsDouble;
    [FieldOffset(0)]
    public readonly long ValueAsLong;

    [FieldOffset(8)]
    public readonly DateTime StartTimeUtc;

    [FieldOffset(16)]
    public readonly DateTime EndTimeUtc;

    public NumberMetricPoint(
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        double value)
    {
        StartTimeUtc = startTimeUtc;
        EndTimeUtc = endTimeUtc;
        ValueAsDouble = value;
    }

    public NumberMetricPoint(
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        long value)
    {
        StartTimeUtc = startTimeUtc;
        EndTimeUtc = endTimeUtc;
        ValueAsLong = value;
    }
}
