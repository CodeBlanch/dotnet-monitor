// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Diagnostics.Monitoring.OpenTelemetry.Logging;
using Microsoft.Diagnostics.Monitoring.OpenTelemetry.Resources;

using OtlpCollectorLogs = OpenTelemetry.Proto.Collector.Logs.V1;
using OtlpCommon = OpenTelemetry.Proto.Common.V1;
using OtlpLogs = OpenTelemetry.Proto.Logs.V1;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.OpenTelemetryProtocol.Logging;

public sealed class OtlpLogRecordExporter : OtlpExporter<OtlpCollectorLogs.ExportLogsServiceRequest, LogRecordBatchWriter>
{
    [ThreadStatic]
    private static OtlpLogRecordWriter? s_Writer;

    private readonly ILogger<OtlpLogRecordExporter> _Logger;

    public OtlpLogRecordExporter(
        ILogger<OtlpLogRecordExporter> logger,
        OtlpExporterOptions options)
        : base(logger, options)
    {
        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override bool Export<TBatch>(
        in TBatch batch,
        CancellationToken cancellationToken)
    {
        var writer = s_Writer ??= new();

        batch.WriteTo(writer, cancellationToken);

        try
        {
            return Send(writer.Request, cancellationToken);
        }
        finally
        {
            writer.Reset();
        }
    }

    private sealed class OtlpLogRecordWriter : LogRecordBatchWriter
    {
        private OtlpLogs.ResourceLogs? _ResourceLogs;
        private OtlpLogs.ScopeLogs? _ScopeLogs;

        public OtlpCollectorLogs.ExportLogsServiceRequest Request { get; private set; }

        public OtlpLogRecordWriter()
        {
            Reset();
        }

        [MemberNotNull(nameof(Request))]
        public void Reset()
        {
            Request = new();
            _ResourceLogs = null;
            _ScopeLogs = null;
        }

        public override void BeginBatch(Resource resource)
        {
            Debug.Assert(resource != null);
            Debug.Assert(_ResourceLogs == null);

            _ResourceLogs = new();

            _ResourceLogs.Resource = new();

            foreach (var resourceAttribute in resource.Attributes)
            {
                _ResourceLogs.Resource.Attributes.Add(
                    new OtlpCommon.KeyValue
                    {
                        Key = resourceAttribute.Key,
                        Value = new()
                        {
                            StringValue = Convert.ToString(resourceAttribute.Value, CultureInfo.InvariantCulture) // todo: handle other types
                        }
                    });
            }
        }

        public override void EndBatch()
        {
            Debug.Assert(_ResourceLogs != null);

            Request.ResourceLogs.Add(_ResourceLogs);
            _ResourceLogs = null;
        }

        public override void BeginInstrumentationScope(InstrumentationScope instrumentationScope)
        {
            Debug.Assert(instrumentationScope != null);
            Debug.Assert(_ResourceLogs != null);
            Debug.Assert(_ScopeLogs == null);

            _ScopeLogs = _ResourceLogs.ScopeLogs.FirstOrDefault(s => s.Scope.Name == instrumentationScope.Name);
            if (_ScopeLogs == null)
            {
                _ScopeLogs = new()
                {
                    Scope = new()
                    {
                        Name = instrumentationScope.Name,
                    }
                };

                if (!string.IsNullOrEmpty(instrumentationScope.Version))
                {
                    _ScopeLogs.Scope.Version = instrumentationScope.Version;
                }

                _ResourceLogs.ScopeLogs.Add(_ScopeLogs);
            }
        }

        public override void EndInstrumentationScope()
        {
            Debug.Assert(_ScopeLogs != null);

            _ScopeLogs = null;
        }

        public override void WriteLogRecord(in LogRecord logRecord)
        {
            Debug.Assert(_ScopeLogs != null);

            var otlpLogRecord = new OtlpLogs.LogRecord
            {
                TimeUnixNano = logRecord.Info.TimestampUtc.ToUnixTimeNanoseconds(),
                SeverityNumber = (OtlpLogs.SeverityNumber)(int)logRecord.Info.Severity,
            };

            if (!string.IsNullOrEmpty(logRecord.Info.Body))
            {
                otlpLogRecord.Body = new OtlpCommon.AnyValue { StringValue = logRecord.Info.Body };
            }

            if (!string.IsNullOrEmpty(logRecord.Info.SeverityText))
            {
                otlpLogRecord.SeverityText = logRecord.Info.SeverityText;
            }

            if (logRecord.Info.TraceId != default && logRecord.Info.SpanId != default)
            {
                byte[] traceIdBytes = new byte[16];
                byte[] spanIdBytes = new byte[8];

                logRecord.Info.TraceId.CopyTo(traceIdBytes);
                logRecord.Info.SpanId.CopyTo(spanIdBytes);

                otlpLogRecord.TraceId = UnsafeByteOperations.UnsafeWrap(traceIdBytes);
                otlpLogRecord.SpanId = UnsafeByteOperations.UnsafeWrap(spanIdBytes);
                otlpLogRecord.Flags = (uint)logRecord.Info.TraceFlags;
            }

            OtlpCommon.OtlpCommonExtensions.AddRange(otlpLogRecord.Attributes, logRecord.Attributes);

            _ScopeLogs.LogRecords.Add(otlpLogRecord);
        }
    }
}
