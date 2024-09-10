// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry.Resources;

public sealed class Resource
{
    public Resource(
        IEnumerable<KeyValuePair<string, object>> attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);

        Attributes = attributes.Select(SanitizeAttribute).ToList();
    }

    public IReadOnlyList<KeyValuePair<string, object>> Attributes { get; }

    private static KeyValuePair<string, object> SanitizeAttribute(
        KeyValuePair<string, object> attribute)
    {
        if (attribute.Key == null)
        {
            throw new NotSupportedException("Resource attributes with null keys are not supported");
        }

        return !TrySanitizeValue(attribute.Value, out var sanitizedValue)
            ? throw new NotSupportedException($"Resource attribute key '{attribute.Key}' value '{attribute.Value}' is not supported")
            : new(attribute.Key, sanitizedValue);
    }

    private static bool TrySanitizeValue(
        object value,
        [NotNullWhen(true)] out object? sanitizedValue)
    {
        sanitizedValue = value switch
        {
            string => value,
            bool => value,
            double => value,
            long => value,
            string[] => value,
            bool[] => value,
            double[] => value,
            long[] => value,
            int => Convert.ToInt64(value),
            short => Convert.ToInt64(value),
            float => Convert.ToDouble(value, CultureInfo.InvariantCulture),
            int[] v => Array.ConvertAll(v, Convert.ToInt64),
            short[] v => Array.ConvertAll(v, Convert.ToInt64),
            float[] v => Array.ConvertAll(v, f => Convert.ToDouble(f, CultureInfo.InvariantCulture)),
            _ => null,
        };

        return sanitizedValue != null;
    }
}
