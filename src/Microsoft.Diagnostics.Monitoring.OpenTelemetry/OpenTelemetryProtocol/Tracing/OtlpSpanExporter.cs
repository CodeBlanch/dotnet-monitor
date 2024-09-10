// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Diagnostics.Monitoring.OpenTelemetry.Tracing;
using Microsoft.Diagnostics.Monitoring.OpenTelemetry.Resources;

using OtlpCollectorTrace = OpenTelemetry.Proto.Collector.Trace.V1;
using OtlpCommon = OpenTelemetry.Proto.Common.V1;
using OtlpTrace = OpenTelemetry.Proto.Trace.V1;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.OpenTelemetryProtocol.Tracing;

public sealed class OtlpSpanExporter : OtlpExporter<OtlpCollectorTrace.ExportTraceServiceRequest, SpanBatchWriter>
{
    [ThreadStatic]
    private static OtlpSpanWriter? s_Writer;

    private readonly ILogger<OtlpSpanExporter> _Logger;

    public OtlpSpanExporter(
        ILogger<OtlpSpanExporter> logger,
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

    private sealed class OtlpSpanWriter : SpanBatchWriter
    {
        private OtlpTrace.ResourceSpans? _ResourceSpans;
        private OtlpTrace.ScopeSpans? _ScopeSpans;

        public OtlpCollectorTrace.ExportTraceServiceRequest Request { get; private set; }

        public OtlpSpanWriter()
        {
            Reset();
        }

        [MemberNotNull(nameof(Request))]
        public void Reset()
        {
            Request = new();
            _ResourceSpans = null;
            _ScopeSpans = null;
        }

        public override void BeginBatch(Resource resource)
        {
            Debug.Assert(resource != null);
            Debug.Assert(_ResourceSpans == null);

            _ResourceSpans = new();

            _ResourceSpans.Resource = new();

            foreach (var resourceAttribute in resource.Attributes)
            {
                _ResourceSpans.Resource.Attributes.Add(
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
            Debug.Assert(_ResourceSpans != null);

            Request.ResourceSpans.Add(_ResourceSpans);
            _ResourceSpans = null;
        }

        public override void BeginInstrumentationScope(InstrumentationScope instrumentationScope)
        {
            Debug.Assert(instrumentationScope != null);
            Debug.Assert(_ResourceSpans != null);
            Debug.Assert(_ScopeSpans == null);

            _ScopeSpans = _ResourceSpans.ScopeSpans.FirstOrDefault(s => s.Scope.Name == instrumentationScope.Name);
            if (_ScopeSpans == null)
            {
                _ScopeSpans = new()
                {
                    Scope = new()
                    {
                        Name = instrumentationScope.Name,
                    }
                };

                if (!string.IsNullOrEmpty(instrumentationScope.Version))
                {
                    _ScopeSpans.Scope.Version = instrumentationScope.Version;
                }

                _ResourceSpans.ScopeSpans.Add(_ScopeSpans);
            }
        }

        public override void EndInstrumentationScope()
        {
            Debug.Assert(_ScopeSpans != null);

            _ScopeSpans = null;
        }

        public override void WriteSpan(in Span span)
        {
            Debug.Assert(_ScopeSpans != null);

            byte[] traceIdBytes = new byte[16];
            byte[] spanIdBytes = new byte[8];

            span.Info.TraceId.CopyTo(traceIdBytes);
            span.Info.SpanId.CopyTo(spanIdBytes);

            var parentSpanIdString = ByteString.Empty;
            if (span.Info.ParentSpanId != default)
            {
                byte[] parentSpanIdBytes = new byte[8];
                span.Info.ParentSpanId.CopyTo(parentSpanIdBytes);
                parentSpanIdString = UnsafeByteOperations.UnsafeWrap(parentSpanIdBytes);
            }

            var otlpSpan = new OtlpTrace.Span()
            {
                Name = span.Info.Name,

                TraceId = UnsafeByteOperations.UnsafeWrap(traceIdBytes),
                SpanId = UnsafeByteOperations.UnsafeWrap(spanIdBytes),
                ParentSpanId = parentSpanIdString,
                TraceState = span.Info.TraceState ?? string.Empty,

                StartTimeUnixNano = span.Info.StartTimestampUtc.ToUnixTimeNanoseconds(),
                EndTimeUnixNano = span.Info.EndTimestampUtc.ToUnixTimeNanoseconds(),
            };

            if (span.Info.Kind.HasValue)
            {
                // There is an offset of 1 on the OTLP enum.
                otlpSpan.Kind = (OtlpTrace.Span.Types.SpanKind)(span.Info.Kind + 1);
            }

            switch (span.Info.StatusCode)
            {
                case ActivityStatusCode.Ok:
                    otlpSpan.Status = new()
                    {
                        Code = OtlpTrace.Status.Types.StatusCode.Ok
                    };
                    break;
                case ActivityStatusCode.Error:
                    otlpSpan.Status = new()
                    {
                        Code = OtlpTrace.Status.Types.StatusCode.Error
                    };
                    if (!string.IsNullOrEmpty(span.Info.StatusDescription))
                    {
                        otlpSpan.Status.Message = span.Info.StatusDescription;
                    }
                    break;
            }

            OtlpCommon.OtlpCommonExtensions.AddRange(otlpSpan.Attributes, span.Attributes);

            _ScopeSpans.Spans.Add(otlpSpan);
        }
    }
}
