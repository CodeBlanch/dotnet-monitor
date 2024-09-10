// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.OpenTelemetryProtocol;

public sealed class OtlpExporterOptions
{
    public OtlpExporterProtocolType? ProtocolType { get; }

    public Uri? Url { get; }

    public IReadOnlyCollection<KeyValuePair<string, string>>? HeaderOptions { get; }

    public OtlpExporterOptions(
        OtlpExporterProtocolType? protocolType,
        Uri? url,
        IReadOnlyCollection<KeyValuePair<string, string>>? headerOptions)
    {
        ProtocolType = protocolType;
        Url = url;
        HeaderOptions = headerOptions;
    }
}
