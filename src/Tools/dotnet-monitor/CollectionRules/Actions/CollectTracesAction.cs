// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectTracesActionFactory :
        ICollectionRuleActionFactory<CollectTracesOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public CollectTracesActionFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public ICollectionRuleAction Create(IProcessInfo processInfo, CollectTracesOptions options)
        {
            if (null == options)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ValidationContext context = new(options, _serviceProvider, items: null);
            Validator.ValidateObject(options, context, validateAllProperties: true);

            return new CollectTracesAction(_serviceProvider, processInfo, options);
        }

        private sealed class CollectTracesAction :
            CollectionRuleEgressActionBase<CollectTracesOptions>
        {
            private readonly IOptionsMonitor<GlobalCounterOptions> _counterOptions;
            private readonly IOptionsMonitor<MetricsOptions> _metricsOptions;
            private readonly ITracesOperationFactory _tracesOperationFactory;

            public CollectTracesAction(IServiceProvider serviceProvider, IProcessInfo processInfo, CollectTracesOptions options)
                : base(serviceProvider, processInfo, options)
            {
                _counterOptions = serviceProvider.GetRequiredService<IOptionsMonitor<GlobalCounterOptions>>();
                _tracesOperationFactory = serviceProvider.GetRequiredService<ITracesOperationFactory>();
                _metricsOptions = serviceProvider.GetRequiredService<IOptionsMonitor<MetricsOptions>>();
            }

            protected override EgressOperation CreateArtifactOperation(CollectionRuleMetadata collectionRuleMetadata)
            {
                TracesPipelineSettings settings = new TracesPipelineSettings()
                {
                    Sources = Options.Sources?.ToArray() ?? Array.Empty<string>(),
                    Duration = Options.Duration.GetValueOrDefault(TimeSpan.Parse(CollectTracesOptionsDefaults.Duration))
                };

                IArtifactOperation operation = _tracesOperationFactory.Create(
                    EndpointInfo,
                    settings);

                KeyValueLogScope scope = Utils.CreateArtifactScope(Utils.ArtifactType_Traces, EndpointInfo);

                EgressOperation egressOperation = new EgressOperation(
                    operation,
                    Options.Egress,
                    ProcessInfo,
                    scope,
                    null,
                    collectionRuleMetadata);

                return egressOperation;
            }
        }
    }
}
