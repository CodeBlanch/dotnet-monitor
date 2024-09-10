// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Tracing;

public readonly ref struct Span
{
    private readonly ref readonly SpanInfo _Info;

    public Span(in SpanInfo info)
    {
        _Info = ref info;
    }

    public readonly ref readonly SpanInfo Info => ref _Info;

    public ReadOnlySpan<KeyValuePair<string, object?>> Attributes { get; init; }

    public ReadOnlySpan<SpanLink> Links { get; init; }

    public ReadOnlySpan<SpanEvent> Events { get; init; }
}
