// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class TracesOperationFactory : ITracesOperationFactory
    {
        private readonly OperationTrackerService _operationTrackerService;
        private readonly ILogger<TracesOperation> _logger;

        public TracesOperationFactory(OperationTrackerService operationTrackerService, ILogger<TracesOperation> logger)
        {
            _operationTrackerService = operationTrackerService;
            _logger = logger;
        }

        public IArtifactOperation Create(IEndpointInfo endpointInfo, TracesPipelineSettings settings)
        {
            return new TracesOperation(endpointInfo, settings, _operationTrackerService, _logger);
        }
    }
}
