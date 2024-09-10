// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class TracesOperation : PipelineArtifactOperation<TracesPipeline>
    {
        private readonly TracesPipelineSettings _settings;

        public TracesOperation(IEndpointInfo endpointInfo, TracesPipelineSettings settings, OperationTrackerService trackerService, ILogger logger)
            : base(trackerService, logger, Utils.ArtifactType_Metrics, endpointInfo)
        {
            _settings = settings;
        }

        protected override TracesPipeline CreatePipeline(Stream outputStream)
        {
            var client = new DiagnosticsClient(EndpointInfo.Endpoint);

            return new TracesPipeline(
                client,
                _settings,
                loggers: new[] { new JsonActivityLogger(outputStream, Logger) });
        }

        protected override Task<Task> StartPipelineAsync(TracesPipeline pipeline, CancellationToken token)
        {
            return pipeline.StartAsync(token);
        }

        public override string GenerateFileName()
        {
            return FormattableString.Invariant($"{Utils.GetFileNameTimeStampUtcNow()}_{EndpointInfo.ProcessId}.traces.json");
        }

        public override string ContentType => ContentTypes.ApplicationJsonSequence;
    }
}
