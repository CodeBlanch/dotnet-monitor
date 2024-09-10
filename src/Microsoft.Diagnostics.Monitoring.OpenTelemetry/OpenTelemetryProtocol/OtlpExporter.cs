// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.OpenTelemetryProtocol;

public abstract class OtlpExporter<TRequest, TBatchWriter> : Exporter<TBatchWriter>
    where TRequest : IMessage
    where TBatchWriter : IBatchWriter
{
    private readonly ILogger _Logger;
    private readonly Uri _RequestUri;
    private readonly HttpClient _HttpClient;
    private readonly IReadOnlyCollection<KeyValuePair<string, string>>? _HeaderOptions;
    private bool _Disposed;

    internal OtlpExporter(
        ILogger logger,
        OtlpExporterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _RequestUri = options.Url ?? throw new ArgumentException("Uri was not specified on optons", nameof(options));
        _HeaderOptions = options.HeaderOptions;

        _HttpClient = new();
    }

    protected bool Send(TRequest payload, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _RequestUri);

            request.Content = new OtlpExporterHttpContent<TRequest>(payload);

            if (_HeaderOptions != null)
            {
                foreach (var header in _HeaderOptions)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            using var response =
#if NET
                _HttpClient.Send(request, cancellationToken);
#else
                _HttpClient.SendAsync(request, cancellationToken).GetAwaiter().GetResult();
#endif

            if (!response.IsSuccessStatusCode)
            {
                _Logger.LogWarning("Error status code '{StatusCode}' returned sending telemetry to '{Endpoint}' endpoint", response.StatusCode, _RequestUri);
                return false;
            }

            _Logger.LogInformation("Telemetry sent successfully to '{Endpoint}' endpoint", _RequestUri);
            return true;
        }
        catch (Exception ex)
        {
            _Logger.LogWarning(ex, "Exception thrown sending telemetry to '{Endpoint}' endpoint", _RequestUri);
            return false;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!_Disposed)
        {
            if (disposing)
            {
                _HttpClient.Dispose();
            }

            _Disposed = true;
        }

        base.Dispose(disposing);
    }
}
