// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.OpenTelemetry;

internal sealed class OpenTelemetryOptionsFactory : OptionsFactory<OpenTelemetryOptions>
{
    private readonly IConfigurationSection _Configuration;

    public OpenTelemetryOptionsFactory(
        IConfigurationSection configuration,
        IEnumerable<IConfigureOptions<OpenTelemetryOptions>> setups,
        IEnumerable<IPostConfigureOptions<OpenTelemetryOptions>> postConfigures,
        IEnumerable<IValidateOptions<OpenTelemetryOptions>> validations)
        : base(setups, postConfigures, validations)
    {
        _Configuration = configuration;
    }

    protected override OpenTelemetryOptions CreateInstance(string name)
    {
        IConfigurationSection config = _Configuration;

        return new(
            OpenTelemetryResourceOptions.ParseFromConfig(config.GetSection("Resource")),
            OpenTelemetryLogsOptions.ParseFromConfig(config.GetSection("Logs")),
            OpenTelemetryMetricsOptions.ParseFromConfig(config.GetSection("Metrics")),
            OpenTelemetryTracesOptions.ParseFromConfig(config.GetSection("Traces")),
            OpenTelemetryExporterOptions.ParseFromConfig(config.GetSection("Exporter")));
    }
}
