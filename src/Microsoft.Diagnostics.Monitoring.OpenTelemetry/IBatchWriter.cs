// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.OpenTelemetry.Resources;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry;

public interface IBatchWriter
{
    void BeginBatch(Resource resource);
    void EndBatch();

    void BeginInstrumentationScope(InstrumentationScope instrumentationScope);
    void EndInstrumentationScope();
}
