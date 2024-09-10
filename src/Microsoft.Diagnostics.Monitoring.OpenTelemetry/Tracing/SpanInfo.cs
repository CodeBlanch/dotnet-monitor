// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Tracing;

public readonly struct SpanInfo
{
    public SpanInfo(
        InstrumentationScope scope,
        string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        Name = name;
    }

    public InstrumentationScope Scope { get; }

    public string Name { get; }

    public ActivityKind? Kind { get; init; }

    public required ActivityTraceId TraceId { get; init; }

    public required ActivitySpanId SpanId { get; init; }

    public required ActivityTraceFlags TraceFlags { get; init; }

    public string? TraceState { get; init; }

    public ActivitySpanId ParentSpanId { get; init; }

    public required DateTime StartTimestampUtc { get; init; }

    public required DateTime EndTimestampUtc { get; init; }

    public ActivityStatusCode StatusCode { get; init; } = ActivityStatusCode.Unset;

    public string? StatusDescription { get; init; }
}
