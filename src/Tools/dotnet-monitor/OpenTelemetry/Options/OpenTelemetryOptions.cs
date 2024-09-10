// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Diagnostics.Monitoring.OpenTelemetry;
using Microsoft.Diagnostics.Monitoring.OpenTelemetry.Metrics;
using Microsoft.Diagnostics.Monitoring.OpenTelemetry.OpenTelemetryProtocol;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.Tools.Monitor.OpenTelemetry;

public class OpenTelemetryOptions
{
    public OpenTelemetryResourceOptions ResourceOptions { get; }

    public OpenTelemetryLogsOptions LogsOptions { get; }

    public OpenTelemetryMetricsOptions MetricsOptions { get; }

    public OpenTelemetryTracesOptions TracesOptions { get; }

    public OpenTelemetryExporterOptions ExporterOptions { get; }

    internal OpenTelemetryOptions(
        OpenTelemetryResourceOptions resourceOptions,
        OpenTelemetryLogsOptions logsOptions,
        OpenTelemetryMetricsOptions metricsOptions,
        OpenTelemetryTracesOptions tracesOptions,
        OpenTelemetryExporterOptions exporterOptions)
    {
        ResourceOptions = resourceOptions;
        LogsOptions = logsOptions;
        MetricsOptions = metricsOptions;
        TracesOptions = tracesOptions;
        ExporterOptions = exporterOptions;
    }
}

public class OpenTelemetryResourceOptions
{
    public IReadOnlyCollection<OpenTelemetryResourceAttributeOptions> AttributeOptions { get; }

    internal OpenTelemetryResourceOptions(
        IReadOnlyCollection<OpenTelemetryResourceAttributeOptions> attributeOptions)
    {
        AttributeOptions = attributeOptions;
    }

    internal static OpenTelemetryResourceOptions ParseFromConfig(IConfigurationSection config)
    {
        List<OpenTelemetryResourceAttributeOptions> options = new();

        foreach (KeyValuePair<string, string?> attribute in config.AsEnumerable(makePathsRelative: true))
        {
            if (string.IsNullOrEmpty(attribute.Key)
                || string.IsNullOrEmpty(attribute.Value))
            {
                continue;
            }

            options.Add(new(attribute.Key, attribute.Value));
        }

        return new(options);
    }
}

public class OpenTelemetryResourceAttributeOptions
{
    public string Key { get; }

    public string ValueOrExpression { get; }

    internal OpenTelemetryResourceAttributeOptions(
        string key,
        string valueOrExpression)
    {
        Key = key;
        ValueOrExpression = valueOrExpression;
    }
}

public class OpenTelemetryMetricsOptions
{
    internal const int DefaultMaxHistograms = 10;
    internal const int DefaultMaxTimeSeries = 1000;

    public int MaxHistograms { get; }

    public int MaxTimeSeries { get; }

    public AggregationTemporality AggregationTemporality { get; }

    public IReadOnlyCollection<OpenTelemetryMetricsMeterOptions> MeterOptions { get; }

    public OpenTelemetryPeriodicExportingOptions PeriodicExportingOptions { get; }

    internal OpenTelemetryMetricsOptions(
        int maxHistograms,
        int maxTimeSeries,
        AggregationTemporality aggregationTemporality,
        IReadOnlyCollection<OpenTelemetryMetricsMeterOptions> meterOptions,
        OpenTelemetryPeriodicExportingOptions periodicExportingOptions)
    {
        MaxHistograms = maxHistograms;
        MaxTimeSeries = maxTimeSeries;
        AggregationTemporality = aggregationTemporality;
        MeterOptions = meterOptions;
        PeriodicExportingOptions = periodicExportingOptions;
    }

    internal static OpenTelemetryMetricsOptions ParseFromConfig(IConfigurationSection config)
    {
        List<OpenTelemetryMetricsMeterOptions> meters = new();

        if (!TryParseIntValue(config, nameof(MaxHistograms), out int maxHistograms))
        {
            maxHistograms = DefaultMaxHistograms;
        }

        if (!TryParseIntValue(config, nameof(MaxTimeSeries), out int maxTimeSeries))
        {
            maxTimeSeries = DefaultMaxTimeSeries;
        }

        AggregationTemporality aggregationTemporality = AggregationTemporality.Cumulative;
        var tempAggregationTemporality = config.GetValue<string?>(nameof(AggregationTemporality), null);
        if (!string.IsNullOrEmpty(tempAggregationTemporality)
            && Enum.TryParse<AggregationTemporality>(tempAggregationTemporality, ignoreCase: true, out var parsedAggregationTemporality))
        {
            aggregationTemporality = parsedAggregationTemporality;
        }

        foreach (IConfigurationSection meter in config.GetSection("Meters").GetChildren())
        {
            if (string.IsNullOrEmpty(meter.Key))
            {
                continue;
            }

            List<string> instruments = new();

            foreach (IConfigurationSection instrument in meter.GetChildren())
            {
                if (string.IsNullOrEmpty(instrument.Value))
                {
                    continue;
                }

                instruments.Add(instrument.Value);
            }

            meters.Add(new(meter.Key, instruments));
        }

        return new(
            maxHistograms,
            maxTimeSeries,
            aggregationTemporality,
            meters,
            OpenTelemetryPeriodicExportingOptions.ParseFromConfig(config.GetSection("PeriodicExporting")));
    }

    private static bool TryParseIntValue(IConfigurationSection config, string key, out int value)
    {
        string? valueString = config[key];
        if (!string.IsNullOrEmpty(valueString)
            && int.TryParse(valueString, out value))
        {
            return true;
        }

        value = default;
        return false;
    }
}

public class OpenTelemetryMetricsMeterOptions
{
    public string MeterName { get; }

    public IReadOnlyCollection<string> Instruments { get; }

    internal OpenTelemetryMetricsMeterOptions(
        string meterName,
        IReadOnlyCollection<string> instruments)
    {
        MeterName = meterName;
        Instruments = instruments;
    }
}

public class OpenTelemetryTracesOptions
{
    [Range(0D, 1D)]
    public double SamplingRatio { get; }

    public IReadOnlyCollection<string> Sources { get; }

    public OpenTelemetryBatchOptions BatchOptions { get; }

    internal OpenTelemetryTracesOptions(
        double samplingRatio,
        IReadOnlyCollection<string> sources,
        OpenTelemetryBatchOptions batchOptions)
    {
        SamplingRatio = samplingRatio;
        Sources = sources;
        BatchOptions = batchOptions;
    }

    internal static OpenTelemetryTracesOptions ParseFromConfig(IConfigurationSection config)
    {
        List<string> sources = new();

        foreach (var source in config.GetSection("Sources").GetChildren())
        {
            if (string.IsNullOrEmpty(source.Value))
            {
                continue;
            }

            sources.Add(source.Value);
        }

        return new(
            config.GetValue("SamplingRatio", 1.0D),
            sources,
            OpenTelemetryBatchOptions.ParseFromConfig(config.GetSection("Batch")));
    }
}

public class OpenTelemetryLogsOptions
{
    public string? DefaultLogLevel { get; }

    public bool IncludeScopes { get; }

    public IReadOnlyCollection<OpenTelemetryLogCategoryOptions> CategoryOptions { get; }

    public OpenTelemetryBatchOptions BatchOptions { get; }

    internal OpenTelemetryLogsOptions(
        string? defaultLogLevel,
        bool includeScopes,
        IReadOnlyCollection<OpenTelemetryLogCategoryOptions> categoryOptions,
        OpenTelemetryBatchOptions batchOptions)
    {
        DefaultLogLevel = defaultLogLevel;
        IncludeScopes = includeScopes;
        CategoryOptions = categoryOptions;
        BatchOptions = batchOptions;
    }

    internal static OpenTelemetryLogsOptions ParseFromConfig(IConfigurationSection config)
    {
        List<OpenTelemetryLogCategoryOptions> categories = new();
        string? defaultLogLevel = null;

        foreach (KeyValuePair<string, string?> category in config.GetSection("Categories").AsEnumerable(makePathsRelative: true))
        {
            if (string.IsNullOrEmpty(category.Key)
                || string.IsNullOrEmpty(category.Value))
            {
                continue;
            }

            if (string.Equals("Default", category.Key, StringComparison.OrdinalIgnoreCase))
            {
                defaultLogLevel = category.Value;
                continue;
            }

            categories.Add(new(category.Key, category.Value));
        }

        return new(
            defaultLogLevel,
            config.GetValue("IncludeScopes", false),
            categories,
            OpenTelemetryBatchOptions.ParseFromConfig(config.GetSection("Batch")));
    }
}

public class OpenTelemetryLogCategoryOptions
{
    public string CategoryPrefix { get; }

    public string LogLevel { get; }

    internal OpenTelemetryLogCategoryOptions(
        string categoryPrefix,
        string logLevel)
    {
        CategoryPrefix = categoryPrefix;
        LogLevel = logLevel;
    }
}

public class OpenTelemetryExporterOptions
{
    public OpenTelemetryExporterType ExporterType { get; }

    public OpenTelemetryProtocolExporterOptions? OpenTelemetryProtocolExporterOptions { get; }

    internal OpenTelemetryExporterOptions(
        OpenTelemetryExporterType exporterType,
        OpenTelemetryProtocolExporterOptions? openTelemetryProtocolExporterOptions)
    {
        ExporterType = exporterType;
        OpenTelemetryProtocolExporterOptions = openTelemetryProtocolExporterOptions;
    }

    internal static OpenTelemetryExporterOptions ParseFromConfig(IConfigurationSection config)
    {
        string? exporterTypeValue = config["Type"];

        OpenTelemetryExporterType exporterType = OpenTelemetryExporterType.Unknown;
        OpenTelemetryProtocolExporterOptions? openTelemetryProtocolExporterOptions = null;

        if (string.Equals(exporterTypeValue, "otlp", StringComparison.OrdinalIgnoreCase))
        {
            exporterType = OpenTelemetryExporterType.OpenTelemetryProtocol;
            openTelemetryProtocolExporterOptions = OpenTelemetryProtocolExporterOptions.ParseFromConfig(
                config.GetSection("Settings"));
        }

        return new(exporterType, openTelemetryProtocolExporterOptions);
    }
}

public enum OpenTelemetryExporterType
{
    Unknown,
    OpenTelemetryProtocol
}

public class OpenTelemetryProtocolExporterOptions
{
    public OpenTelemetryProtocolExporterSignalOptions DefaultOptions { get; }

    public OpenTelemetryProtocolExporterSignalOptions LogsOptions { get; }

    public OpenTelemetryProtocolExporterSignalOptions MetricsOptions { get; }

    public OpenTelemetryProtocolExporterSignalOptions TracesOptions { get; }

    internal OpenTelemetryProtocolExporterOptions(
        OpenTelemetryProtocolExporterSignalOptions defaultOptions,
        OpenTelemetryProtocolExporterSignalOptions logsOptions,
        OpenTelemetryProtocolExporterSignalOptions metricsOptions,
        OpenTelemetryProtocolExporterSignalOptions tracesOptions)
    {
        DefaultOptions = defaultOptions;
        LogsOptions = logsOptions;
        MetricsOptions = metricsOptions;
        TracesOptions = tracesOptions;
    }

    internal static OpenTelemetryProtocolExporterOptions ParseFromConfig(IConfigurationSection config)
    {
        return new(
            OpenTelemetryProtocolExporterSignalOptions.ParseFromConfig(config.GetSection("Defaults")),
            OpenTelemetryProtocolExporterSignalOptions.ParseFromConfig(config.GetSection("Logs")),
            OpenTelemetryProtocolExporterSignalOptions.ParseFromConfig(config.GetSection("Metrics")),
            OpenTelemetryProtocolExporterSignalOptions.ParseFromConfig(config.GetSection("Traces")));
    }

    internal OtlpExporterOptions ResolveOtlpExporterOptions(
        Uri defaultUri,
        OpenTelemetryProtocolExporterSignalOptions signalOptions)
    {
        var requestUri = ResolveUrl(signalOptions, DefaultOptions, defaultUri);
        var protocol = signalOptions.ProtocolType ?? DefaultOptions.ProtocolType ?? OpenTelemetryProtocolExporterProtocolType.HttpProtobuf;
        var headers = signalOptions.HeaderOptions ?? DefaultOptions.HeaderOptions;

        return new OtlpExporterOptions(
            (OtlpExporterProtocolType)(int)protocol,
            requestUri,
            headers?.Select(h => new KeyValuePair<string, string>(h.Key, h.Value)).ToList());
    }

    private static Uri ResolveUrl(
        OpenTelemetryProtocolExporterSignalOptions signalOptions,
        OpenTelemetryProtocolExporterSignalOptions defaultOptions,
        Uri defaultUri)
    {
        return signalOptions.Url
            ?? AppedPathToUrl(defaultOptions.Url, defaultUri.PathAndQuery)
            ?? defaultUri;
    }

    [return: NotNullIfNotNull(nameof(uri))]
    private static Uri? AppedPathToUrl(Uri? uri, string path)
    {
        if (uri == null)
            return null;

        var absoluteUri = uri.AbsoluteUri;

        if (!absoluteUri.EndsWith('/'))
            absoluteUri += "/";

        if (path.StartsWith('/'))
            path = path.Substring(1);

        return uri == null
            ? null
            : new Uri(string.Concat(absoluteUri, path));
    }
}

public class OpenTelemetryProtocolExporterSignalOptions
{
    public OpenTelemetryProtocolExporterProtocolType? ProtocolType { get; }

    public Uri? Url { get; }

    public IReadOnlyCollection<OpenTelemetryProtocolExporterHeaderOptions>? HeaderOptions { get; }

    internal OpenTelemetryProtocolExporterSignalOptions(
        OpenTelemetryProtocolExporterProtocolType? protocolType,
        Uri? url,
        IReadOnlyCollection<OpenTelemetryProtocolExporterHeaderOptions>? headerOptions)
    {
        ProtocolType = protocolType;
        Url = url;
        HeaderOptions = headerOptions;
    }

    internal static OpenTelemetryProtocolExporterSignalOptions ParseFromConfig(IConfigurationSection config)
    {
        OpenTelemetryProtocolExporterProtocolType? protocol = null;
        Uri? url = null;

        string? protocolValue = config["Protocol"];
        if (!string.IsNullOrEmpty(protocolValue)
            && Enum.TryParse<OpenTelemetryProtocolExporterProtocolType>(protocolValue, ignoreCase: true, out OpenTelemetryProtocolExporterProtocolType tempProtocol))
        {
            protocol = tempProtocol;
        }

        string? urlValue = config["BaseUrl"];
        if (string.IsNullOrEmpty(urlValue))
            urlValue = config["Url"];

        if (!string.IsNullOrEmpty(urlValue)
            && Uri.TryCreate(urlValue, UriKind.Absolute, out Uri? tempUrl))
        {
            url = tempUrl;
        }

        List<OpenTelemetryProtocolExporterHeaderOptions>? headers = null;

        foreach (KeyValuePair<string, string?> header in config.GetSection("Headers").AsEnumerable(makePathsRelative: true))
        {
            if (string.IsNullOrEmpty(header.Key)
                || string.IsNullOrEmpty(header.Value))
            {
                continue;
            }

            (headers ??= new()).Add(new(header.Key, header.Value));
        }

        return new(protocol, url, headers);
    }
}

public class OpenTelemetryProtocolExporterHeaderOptions
{
    public string Key { get; }

    public string Value { get; }

    internal OpenTelemetryProtocolExporterHeaderOptions(
        string key,
        string value)
    {
        Key = key;
        Value = value;
    }
}

public enum OpenTelemetryProtocolExporterProtocolType
{
    HttpProtobuf
}

public class OpenTelemetryBatchOptions
{
    internal const int DefaultMaxQueueSize = 2048;
    internal const int DefaultMaxExportBatchSize = 512;
    internal const int DefaultExportIntervalMilliseconds = 5000;
    internal const int DefaultExportTimeoutMilliseconds = 30000;

    /// <summary>
    /// Gets or sets the maximum queue size. The queue drops the data if the maximum size is reached. The default value is 2048.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxQueueSize { get; }

    /// <summary>
    /// Gets or sets the maximum batch size of every export. It must be smaller or equal to MaxQueueLength. The default value is 512.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxExportBatchSize { get; }

    /// <summary>
    /// Gets or sets the delay interval (in milliseconds) between two consecutive exports. The default value is 5000.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int ExportIntervalMilliseconds { get; }

    /// <summary>
    /// Gets or sets the timeout (in milliseconds) after which the export is cancelled. The default value is 30000.
    /// </summary>
    /// <remarks>Note: Set to <c>-1</c> to disable timeout.</remarks>
    [Range(1, int.MaxValue)]
    public int ExportTimeoutMilliseconds { get; }

    internal OpenTelemetryBatchOptions(
        int maxQueueSize,
        int maxExportBatchSize,
        int exportIntervalMilliseconds,
        int exportTimeoutMilliseconds)
    {
        MaxQueueSize = maxQueueSize;
        MaxExportBatchSize = maxExportBatchSize;
        ExportIntervalMilliseconds = exportIntervalMilliseconds;
        ExportTimeoutMilliseconds = exportTimeoutMilliseconds;
    }

    internal static OpenTelemetryBatchOptions ParseFromConfig(IConfigurationSection config)
    {
        if (!TryParseIntValue(config, nameof(MaxQueueSize), out int maxQueueSize))
        {
            maxQueueSize = DefaultMaxQueueSize;
        }

        if (!TryParseIntValue(config, nameof(MaxExportBatchSize), out int maxExportBatchSize))
        {
            maxExportBatchSize = DefaultMaxExportBatchSize;
        }

        if (!TryParseIntValue(config, nameof(ExportIntervalMilliseconds), out int exportIntervalMilliseconds))
        {
            exportIntervalMilliseconds = DefaultExportIntervalMilliseconds;
        }

        if (!TryParseIntValue(config, nameof(ExportTimeoutMilliseconds), out int exportTimeoutMilliseconds))
        {
            exportTimeoutMilliseconds = DefaultExportTimeoutMilliseconds;
        }

        return new(maxQueueSize, maxExportBatchSize, exportIntervalMilliseconds, exportTimeoutMilliseconds);
    }

    internal BatchExportProcessorOptions ToOTelBatchOptions()
    {
        return new(MaxQueueSize, MaxExportBatchSize, ExportIntervalMilliseconds, ExportTimeoutMilliseconds);
    }

    private static bool TryParseIntValue(IConfigurationSection config, string key, out int value)
    {
        string? valueString = config[key];
        if (!string.IsNullOrEmpty(valueString)
            && int.TryParse(valueString, out value))
        {
            return true;
        }

        value = default;
        return false;
    }
}

public class OpenTelemetryPeriodicExportingOptions
{
    internal const int DefaultExportIntervalMilliseconds = 60000;
    internal const int DefaultExportTimeoutMilliseconds = 30000;

    /// <summary>
    /// Gets or sets the delay interval (in milliseconds) between two consecutive exports. The default value is 60000.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int ExportIntervalMilliseconds { get; }

    /// <summary>
    /// Gets or sets the timeout (in milliseconds) after which the export is cancelled. The default value is 30000.
    /// </summary>
    /// <remarks>Note: Set to <c>-1</c> to disable timeout.</remarks>
    [Range(1, int.MaxValue)]
    public int ExportTimeoutMilliseconds { get; }

    internal OpenTelemetryPeriodicExportingOptions(
        int exportIntervalMilliseconds,
        int exportTimeoutMilliseconds)
    {
        ExportIntervalMilliseconds = exportIntervalMilliseconds;
        ExportTimeoutMilliseconds = exportTimeoutMilliseconds;
    }

    internal static OpenTelemetryPeriodicExportingOptions ParseFromConfig(IConfigurationSection config)
    {
        if (!TryParseIntValue(config, nameof(ExportIntervalMilliseconds), out int exportIntervalMilliseconds))
        {
            exportIntervalMilliseconds = DefaultExportIntervalMilliseconds;
        }

        if (!TryParseIntValue(config, nameof(ExportTimeoutMilliseconds), out int exportTimeoutMilliseconds))
        {
            exportTimeoutMilliseconds = DefaultExportTimeoutMilliseconds;
        }

        return new(exportIntervalMilliseconds, exportTimeoutMilliseconds);
    }

    internal PeriodicExportingMetricReaderOptions ToOTelPeriodicExportingOptions()
    {
        return new(ExportIntervalMilliseconds, ExportTimeoutMilliseconds);
    }

    private static bool TryParseIntValue(IConfigurationSection config, string key, out int value)
    {
        string? valueString = config[key];
        if (!string.IsNullOrEmpty(valueString)
            && int.TryParse(valueString, out value))
        {
            return true;
        }

        value = default;
        return false;
    }
}
