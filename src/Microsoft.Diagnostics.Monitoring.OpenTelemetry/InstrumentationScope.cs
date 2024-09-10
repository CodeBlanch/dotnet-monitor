// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry;

public sealed class InstrumentationScope : IEquatable<InstrumentationScope>
{
    public string Name { get; }

    public string? Version { get; init; }

    public InstrumentationScope(
        string name)
    {
        Name = name ?? string.Empty;
    }

    public bool Equals(InstrumentationScope? other)
        => string.Equals(Name, other?.Name, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj)
        => Equals(obj as InstrumentationScope);

    public override int GetHashCode()
        => Name.GetHashCode();

    public static bool operator ==(InstrumentationScope? left, InstrumentationScope? right)
    {
        return left is null
            ? right is null
            : left.Equals(right);
    }

    public static bool operator !=(InstrumentationScope? left, InstrumentationScope? right)
        => !(left == right);
}
