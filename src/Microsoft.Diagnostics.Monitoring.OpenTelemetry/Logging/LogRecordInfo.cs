// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Logging;

public readonly struct LogRecordInfo
{
    public LogRecordInfo(InstrumentationScope scope)
    {
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
    }

    public InstrumentationScope Scope { get; }

    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    public LogRecordSeverity Severity { get; init; } = LogRecordSeverity.Unspecified;

    public string? SeverityText { get; init; }

    public string? Body { get; init; }

    public ActivityTraceId TraceId { get; init; }

    public ActivitySpanId SpanId { get; init; }

    public ActivityTraceFlags TraceFlags { get; init; }
}
