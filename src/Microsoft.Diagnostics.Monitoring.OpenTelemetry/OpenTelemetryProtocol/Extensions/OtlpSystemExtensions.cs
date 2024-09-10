// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

internal static class OtlpSystemExtensions
{
    private const long NanosecondsPerTicks = 100;
    private const long UnixEpochTicks = 621355968000000000; // = DateTimeOffset.FromUnixTimeMilliseconds(0).Ticks

    public static ulong ToUnixTimeNanoseconds(this DateTime value)
    {
        return (ulong)((value.Ticks - UnixEpochTicks) * NanosecondsPerTicks);
    }
}
