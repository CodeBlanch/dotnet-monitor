// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry;

public interface IBatch<TBatchWriter>
    where TBatchWriter : IBatchWriter
{
    bool WriteTo(TBatchWriter writer, CancellationToken cancellationToken);
}
