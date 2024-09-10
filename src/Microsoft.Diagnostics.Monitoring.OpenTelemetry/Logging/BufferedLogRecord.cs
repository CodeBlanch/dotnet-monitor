// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Logging;

internal sealed class BufferedLogRecord : IBufferedTelemetry<BufferedLogRecord>
{
    private LogRecordInfo _Info;

    private List<KeyValuePair<string, object?>>? _Attributes;

    public InstrumentationScope Scope => _Info.Scope;

    public BufferedLogRecord? Next { get; set; }

    public BufferedLogRecord(in LogRecord logRecord)
    {
        _Info = logRecord.Info;

        SetAttributes(logRecord.Attributes);
    }

    public ref LogRecordInfo Info => ref _Info;

    public ReadOnlySpan<KeyValuePair<string, object?>> GetAttributes()
        => CollectionsMarshal.AsSpan(_Attributes);

    public void SetAttributes(params ReadOnlySpan<KeyValuePair<string, object?>> attributes)
    {
        _Attributes = [.. attributes];
    }

    public void ToLogRecord(out LogRecord logRecord)
    {
        logRecord = new LogRecord(in Info)
        {
            Attributes = GetAttributes()
        };
    }
}
