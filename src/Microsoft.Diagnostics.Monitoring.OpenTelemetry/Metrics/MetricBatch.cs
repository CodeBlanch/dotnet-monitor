// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.OpenTelemetry.Resources;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Metrics;

internal readonly ref struct MetricBatch : IBatch<MetricBatchWriter>
{
    private readonly ILogger _Logger;
    private readonly Resource _Resource;
    private readonly MetricProducer[] _Producers;

    public MetricBatch(
        ILogger logger,
        Resource resource,
        MetricProducer[] producers)
    {
        Debug.Assert(logger != null);
        Debug.Assert(resource != null);
        Debug.Assert(producers != null);

        _Logger = logger;
        _Resource = resource;
        _Producers = producers;
    }

    public bool WriteTo(
        MetricBatchWriter writer,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.BeginBatch(_Resource);

        foreach (var producer in _Producers)
        {
            try
            {
                var result = producer.Produce(writer, cancellationToken);

                _Logger.LogInformation("Telemetry collection completed with {Result} using '{ProducerType}' producer", result, producer.GetType());
            }
            catch (Exception ex)
            {
                _Logger.LogWarning(ex, "Exception thrown collecting telemetry using '{ProducerType}' producer", producer.GetType());
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }
        }

        writer.EndBatch();

        return true;
    }
}
