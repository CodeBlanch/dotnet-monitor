// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// Factory for creating operations that produce traces artifacts.
    /// </summary>
    internal interface ITracesOperationFactory
    {
        /// <summary>
        /// Creates an operation that produces a traces artifact.
        /// </summary>
        IArtifactOperation Create(
            IEndpointInfo endpointInfo,
            TracesPipelineSettings settings);
    }
}
