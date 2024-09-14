// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Original copyright notice from OpenTelemetry repo:
// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Metrics;

/// <summary>
/// Enumeration used to define the aggregation temporality for a <see
/// cref="Metric"/>.
/// </summary>
public enum AggregationTemporality : byte
{
    Unspecified = 0,

    /// <summary>
    /// Delta.
    /// </summary>
    Delta = 1,

    /// <summary>
    /// Cumulative.
    /// </summary>
    Cumulative = 2,
}
