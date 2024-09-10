// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.OpenTelemetry.Resources;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Metrics;

public static class MetricReaderFactory
{
    public static MetricReader CreatePeriodicExportingMetricReader(
        ILoggerFactory loggerFactory,
        Resource resource,
        Exporter<MetricBatchWriter> exporter,
        IEnumerable<MetricProducer>? metricProducers,
        PeriodicExportingMetricReaderOptions options)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        return new PeriodicExportingMetricReader(
            loggerFactory.CreateLogger<PeriodicExportingMetricReader>(),
            resource,
            exporter,
            metricProducers,
            options);
    }
}
