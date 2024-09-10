// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Metrics;

[Flags]
public enum HistogramMetricPointFeatures : byte
{
    None = 0,
    MinAndMax = 0b1,
    Buckets = 0b10
}

