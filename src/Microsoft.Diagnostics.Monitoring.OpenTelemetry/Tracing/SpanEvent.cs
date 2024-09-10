// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Tracing;

public readonly struct SpanEvent
{
    private readonly TagList _Attributes;

    public string Name { get; }

    public DateTime TimestampUtc { get; }

    [UnscopedRef]
    public readonly ref readonly TagList Attributes => ref _Attributes;

    public SpanEvent(
        string name)
        : this(name, DateTime.UtcNow)
    {
    }

    public SpanEvent(
        string name,
        params ReadOnlySpan<KeyValuePair<string, object?>> attributes)
        : this(name, DateTime.UtcNow, attributes)
    {
    }

    public SpanEvent(
        string name,
        DateTime timestampUtc,
        params ReadOnlySpan<KeyValuePair<string, object?>> attributes)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (timestampUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("TimestampUtc kind is invalid", nameof(timestampUtc));
        }

        Name = name;
        TimestampUtc = timestampUtc;
        _Attributes = new TagList(attributes);
    }
}
