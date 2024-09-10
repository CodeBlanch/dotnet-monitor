// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Google.Protobuf.Collections;
using System.Globalization;

namespace OpenTelemetry.Proto.Common.V1;

internal static class OtlpCommonExtensions
{
    public static void AddRange(
        this RepeatedField<KeyValue> otlpAttributes,
        ReadOnlySpan<KeyValuePair<string, object?>> attributes)
    {
        foreach (var attribute in attributes)
        {
            otlpAttributes.Add(
                new KeyValue()
                {
                    Key = attribute.Key,
                    Value = new AnyValue()
                    {
                        StringValue = Convert.ToString(attribute.Value, CultureInfo.InvariantCulture) // todo: support other types
                    }
                });
        }
    }
}
