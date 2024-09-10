// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry;

public abstract class Exporter<TBatchWriter> : IDisposable
    where TBatchWriter : IBatchWriter
{
    protected Exporter()
    {
    }

    public abstract bool Export<TBatch>(
        in TBatch batch,
        CancellationToken cancellationToken)
        where TBatch : IBatch<TBatchWriter>, allows ref struct;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }
}
