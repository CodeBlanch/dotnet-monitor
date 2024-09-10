// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Diagnostics.Monitoring.OpenTelemetry.Metrics;
using Microsoft.Diagnostics.Monitoring.OpenTelemetry.Resources;
using Microsoft.Extensions.Logging;

using OtlpCollectorMetrics = OpenTelemetry.Proto.Collector.Metrics.V1;
using OtlpCommon = OpenTelemetry.Proto.Common.V1;
using OtlpMetrics = OpenTelemetry.Proto.Metrics.V1;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.OpenTelemetryProtocol.Metrics;

public sealed class OtlpMetricExporter : OtlpExporter<OtlpCollectorMetrics.ExportMetricsServiceRequest, MetricBatchWriter>
{
    [ThreadStatic]
    private static OtlpMetricWriter? s_Writer;

    private readonly ILogger<OtlpMetricExporter> _Logger;

    public OtlpMetricExporter(
        ILogger<OtlpMetricExporter> logger,
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

    private sealed class OtlpMetricWriter : MetricBatchWriter
    {
        private OtlpMetrics.ResourceMetrics? _ResourceMetrics;
        private OtlpMetrics.ScopeMetrics? _ScopeMetrics;
        private OtlpMetrics.Metric? _Metric;
        private MetricType _MetricType;

        public OtlpCollectorMetrics.ExportMetricsServiceRequest Request { get; private set; }

        public OtlpMetricWriter()
        {
            Reset();
        }

        [MemberNotNull(nameof(Request))]
        public void Reset()
        {
            Request = new();
            _ResourceMetrics = null;
            _ScopeMetrics = null;
            _Metric = null;
        }

        public override void BeginBatch(Resource resource)
        {
            Debug.Assert(resource != null);
            Debug.Assert(_ResourceMetrics == null);

            _ResourceMetrics = new();

            _ResourceMetrics.Resource = new();

            foreach (var resourceAttribute in resource.Attributes)
            {
                _ResourceMetrics.Resource.Attributes.Add(
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
            Debug.Assert(_ResourceMetrics != null);

            Request.ResourceMetrics.Add(_ResourceMetrics);
            _ResourceMetrics = null;
        }

        public override void BeginInstrumentationScope(InstrumentationScope instrumentationScope)
        {
            Debug.Assert(instrumentationScope != null);
            Debug.Assert(_ResourceMetrics != null);
            Debug.Assert(_ScopeMetrics == null);

            _ScopeMetrics = _ResourceMetrics.ScopeMetrics.FirstOrDefault(s => s.Scope.Name == instrumentationScope.Name);
            if (_ScopeMetrics == null)
            {
                _ScopeMetrics = new()
                {
                    Scope = new()
                    {
                        Name = instrumentationScope.Name,
                    }
                };

                if (!string.IsNullOrEmpty(instrumentationScope.Version))
                {
                    _ScopeMetrics.Scope.Version = instrumentationScope.Version;
                }

                _ResourceMetrics.ScopeMetrics.Add(_ScopeMetrics);
            }
        }

        public override void EndInstrumentationScope()
        {
            Debug.Assert(_ScopeMetrics != null);

            _ScopeMetrics = null;
        }

        public override void BeginMetric(Metric metric)
        {
            Debug.Assert(metric != null);
            Debug.Assert(_ResourceMetrics != null);
            Debug.Assert(_ScopeMetrics != null);
            Debug.Assert(_Metric == null);

            _MetricType = metric.MetricType;

            _Metric = new OtlpMetrics.Metric
            {
                Name = metric.Name
            };

            if (metric.Description != null)
            {
                _Metric.Description = metric.Description;
            }

            if (metric.Unit != null)
            {
                _Metric.Unit = metric.Unit;
            }

            OtlpMetrics.AggregationTemporality aggregationTemporality = (OtlpMetrics.AggregationTemporality)(int)metric.AggregationTemporality;

            switch (metric.MetricType)
            {
                case MetricType.LongSum:
                case MetricType.LongSumNonMonotonic:
                case MetricType.DoubleSum:
                case MetricType.DoubleSumNonMonotonic:
                    _Metric.Sum = new()
                    {
                        IsMonotonic = ((byte)metric.MetricType & 0x80) == 0,
                        AggregationTemporality = aggregationTemporality,
                    };
                    break;
                case MetricType.LongGauge:
                case MetricType.DoubleGauge:
                    _Metric.Gauge = new();
                    break;
                case MetricType.Histogram:
                    _Metric.Histogram = new()
                    {
                        AggregationTemporality = aggregationTemporality
                    };
                    break;
                case MetricType.Summary:
                    _Metric.Summary = new();
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public override void EndMetric()
        {
            Debug.Assert(_ScopeMetrics != null);
            Debug.Assert(_Metric != null);

            _ScopeMetrics.Metrics.Add(_Metric);
            _Metric = null;
        }

        public override void WriteNumberMetricPoint(
            in NumberMetricPoint numberMetricPoint,
            ReadOnlySpan<KeyValuePair<string, object?>> attributes)
        {
            Debug.Assert(_Metric != null);

            var dataPoint = new OtlpMetrics.NumberDataPoint
            {
                StartTimeUnixNano = numberMetricPoint.StartTimeUtc.ToUnixTimeNanoseconds(),
                TimeUnixNano = numberMetricPoint.EndTimeUtc.ToUnixTimeNanoseconds(),
            };

            if (((byte)_MetricType & 0x0c) == 0)
            {
                dataPoint.AsInt = numberMetricPoint.ValueAsLong;
            }
            else
            {
                dataPoint.AsDouble = numberMetricPoint.ValueAsDouble;
            }

            OtlpCommon.OtlpCommonExtensions.AddRange(dataPoint.Attributes, attributes);

            /*if (metricPoint.TryGetExemplars(out var exemplars))
            {
                foreach (ref readonly var exemplar in exemplars)
                {
                    dataPoint.Exemplars.Add(
                        ToOtlpExemplar(exemplar.DoubleValue, in exemplar));
                }
            }*/

            if (_Metric.Sum != null)
            {
                _Metric.Sum.DataPoints.Add(dataPoint);
            }
            else if (_Metric.Gauge != null)
            {
                _Metric.Gauge.DataPoints.Add(dataPoint);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public override void WriteHistogramMetricPoint(
            in HistogramMetricPoint histogramMetricPoint,
            ReadOnlySpan<HistogramMetricPointBucket> buckets,
            ReadOnlySpan<KeyValuePair<string, object?>> attributes)
        {
            Debug.Assert(_Metric?.Histogram != null);

            var dataPoint = new OtlpMetrics.HistogramDataPoint
            {
                StartTimeUnixNano = histogramMetricPoint.StartTimeUtc.ToUnixTimeNanoseconds(),
                TimeUnixNano = histogramMetricPoint.EndTimeUtc.ToUnixTimeNanoseconds(),
            };

            OtlpCommon.OtlpCommonExtensions.AddRange(dataPoint.Attributes, attributes);

            dataPoint.Count = (ulong)histogramMetricPoint.Count;
            dataPoint.Sum = histogramMetricPoint.Sum;

            if (histogramMetricPoint.Features.HasFlag(HistogramMetricPointFeatures.MinAndMax))
            {
                dataPoint.Min = histogramMetricPoint.Min;
                dataPoint.Max = histogramMetricPoint.Max;
            }

            if (histogramMetricPoint.Features.HasFlag(HistogramMetricPointFeatures.Buckets))
            {
                foreach (ref readonly var bucket in buckets)
                {
                    dataPoint.BucketCounts.Add((ulong)bucket.Count);
                    if (bucket.Value != double.PositiveInfinity)
                    {
                        dataPoint.ExplicitBounds.Add(bucket.Value);
                    }
                }
            }

            /*if (metricPoint.TryGetExemplars(out var exemplars))
            {
                foreach (ref readonly var exemplar in exemplars)
                {
                    dataPoint.Exemplars.Add(
                        ToOtlpExemplar(exemplar.DoubleValue, in exemplar));
                }
            }*/

            _Metric.Histogram.DataPoints.Add(dataPoint);
        }

        public override void WriteSummaryMetricPoint(
            in SummaryMetricPoint summaryMetricPoint,
            ReadOnlySpan<SummaryMetricPointQuantile> quantiles,
            ReadOnlySpan<KeyValuePair<string, object?>> attributes)
        {
            Debug.Assert(_Metric?.Summary != null);

            var dataPoint = new OtlpMetrics.SummaryDataPoint
            {
                StartTimeUnixNano = summaryMetricPoint.StartTimeUtc.ToUnixTimeNanoseconds(),
                TimeUnixNano = summaryMetricPoint.EndTimeUtc.ToUnixTimeNanoseconds(),
            };

            OtlpCommon.OtlpCommonExtensions.AddRange(dataPoint.Attributes, attributes);

            dataPoint.Count = (ulong)summaryMetricPoint.Count;
            dataPoint.Sum = summaryMetricPoint.Sum;

            foreach (ref readonly var quantile in quantiles)
            {
                dataPoint.QuantileValues.Add(
                    new OtlpMetrics.SummaryDataPoint.Types.ValueAtQuantile()
                    {
                        Quantile = quantile.Quantile,
                        Value = quantile.Value
                    });
            }

            _Metric.Summary.DataPoints.Add(dataPoint);
        }
    }
}
