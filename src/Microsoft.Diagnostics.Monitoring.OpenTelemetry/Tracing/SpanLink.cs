// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Tracing;

public readonly struct SpanLink
{
    private readonly ActivityContext _SpanContext;
    private readonly TagList _Attributes;

    [UnscopedRef]
    public readonly ref readonly ActivityContext SpanContext => ref _SpanContext;

    [UnscopedRef]
    public readonly ref readonly TagList Attributes => ref _Attributes;

    public SpanLink(
        in ActivityContext spanContext,
        params ReadOnlySpan<KeyValuePair<string, object?>> attributes)
    {
        _SpanContext = spanContext;
        _Attributes = new TagList(attributes);
    }
}
