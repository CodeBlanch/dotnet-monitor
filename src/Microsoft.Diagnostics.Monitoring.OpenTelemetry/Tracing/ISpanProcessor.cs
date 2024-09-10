// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Tracing;

public interface ISpanProcessor : IProcessor
{
    /*
    void ProcessStartedSpan(Activity activity);
    void ProcessEndingSpan(Activity activity);
    */

    void ProcessEndedSpan(in Span span);
}
