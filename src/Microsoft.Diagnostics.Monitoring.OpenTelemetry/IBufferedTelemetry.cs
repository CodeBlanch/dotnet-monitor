// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry;

internal interface IBufferedTelemetry<T>
    where T : class
{
    InstrumentationScope Scope { get; }

    T? Next { get; set; }
}
