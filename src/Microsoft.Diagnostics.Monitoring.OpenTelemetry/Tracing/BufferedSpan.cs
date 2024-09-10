// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Tracing;

internal sealed class BufferedSpan : IBufferedTelemetry<BufferedSpan>
{
    private SpanInfo _Info;

    private List<KeyValuePair<string, object?>>? _Attributes;
    private List<SpanLink>? _Links;
    private List<SpanEvent>? _Events;

    public InstrumentationScope Scope => _Info.Scope;

    public BufferedSpan? Next { get; set; }

    public BufferedSpan(in Span span)
    {
        _Info = span.Info;

        SetAttributes(span.Attributes);
    }

    public ref SpanInfo Info => ref _Info;

    public ReadOnlySpan<KeyValuePair<string, object?>> GetAttributes()
        => CollectionsMarshal.AsSpan(_Attributes);

    public ReadOnlySpan<SpanLink> GetLinks()
        => CollectionsMarshal.AsSpan(_Links);

    public ReadOnlySpan<SpanEvent> GetEvents()
        => CollectionsMarshal.AsSpan(_Events);

    public void SetAttributes(params ReadOnlySpan<KeyValuePair<string, object?>> attributes)
    {
        _Attributes = [.. attributes];
    }

    public void SetLinks(params ReadOnlySpan<SpanLink> links)
    {
        _Links = [.. links];
    }

    public void SetEvents(params ReadOnlySpan<SpanEvent> events)
    {
        _Events = [.. events];
    }

    public void ToSpan(out Span span)
    {
        span = new Span(in Info)
        {
            Attributes = GetAttributes(),
            Links = GetLinks(),
            Events = GetEvents()
        };
    }
}
