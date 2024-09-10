// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Logging;

public readonly ref struct LogRecord
{
    private readonly ref readonly LogRecordInfo _Info;

    public LogRecord(in LogRecordInfo info)
    {
        _Info = ref info;
    }

    public readonly ref readonly LogRecordInfo Info => ref _Info;

    public ReadOnlySpan<KeyValuePair<string, object?>> Attributes { get; init; }
}
