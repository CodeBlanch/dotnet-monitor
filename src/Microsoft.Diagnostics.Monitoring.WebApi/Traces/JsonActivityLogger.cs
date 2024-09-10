// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal sealed class JsonActivityLogger : IActivityLogger
    {
        private const int ActivityBacklog = 1000;

        private static readonly ReadOnlyMemory<byte> JsonSequenceRecordSeparator =
            new byte[] { StreamingLogger.JsonSequenceRecordSeparator };

        private readonly Stream _outputStream;
        private readonly ILogger _logger;
        private readonly Channel<Activity> _channel;
        private readonly ChannelReader<Activity> _channelReader;
        private readonly ChannelWriter<Activity> _channelWriter;
        private Task? _processingTask;
        private long _dropCount;

        public JsonActivityLogger(Stream outputStream, ILogger logger)
        {
            _outputStream = outputStream;
            _logger = logger;

            _channel = Channel.CreateBounded<Activity>(
                new BoundedChannelOptions(ActivityBacklog)
                {
                    AllowSynchronousContinuations = false,
                    FullMode = BoundedChannelFullMode.DropWrite,
                    SingleReader = true,
                    SingleWriter = true
                },
                ChannelItemDropped);
            _channelReader = _channel.Reader;
            _channelWriter = _channel.Writer;
        }

        public void Log(
            in ActivityData activity,
            ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            _channelWriter.TryWrite(new(in activity, tags.ToArray()));
        }

        public Task PipelineStarted(CancellationToken token)
        {
            _processingTask = ReadAndSerializeAsync(token);
            return Task.CompletedTask;
        }

        public async Task PipelineStopped(CancellationToken token)
        {
            _channelWriter.Complete();

            if (_dropCount > 0)
            {
                _logger.ActivitiesDropped(_dropCount);
            }

            await (_processingTask ?? Task.CompletedTask);

            try
            {
                int pendingCount = _channelReader.Count;
                if (pendingCount > 0)
                {
                    _logger.ActivitiesUnprocessed(pendingCount);
                }
            }
            catch (Exception)
            {
            }
        }

        private void ChannelItemDropped(Activity activity)
        {
            _dropCount++;
        }

        private async Task ReadAndSerializeAsync(CancellationToken token)
        {
            Utf8JsonWriter writer = new Utf8JsonWriter(
                _outputStream,
                new JsonWriterOptions
                {
#if RELEASE
                    SkipValidation = true,
#endif
                    Indented = false
                });

            try
            {
                while (await _channelReader.WaitToReadAsync(token))
                {
                    await _outputStream.WriteAsync(JsonSequenceRecordSeparator, token);

                    Serialize(writer, await _channelReader.ReadAsync(token));

                    await writer.FlushAsync(token);
                    writer.Reset(_outputStream);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.ActivitiesWriteFailed(ex);
            }
        }

        private static void Serialize(Utf8JsonWriter writer, Activity activity)
        {
            ref readonly ActivityData data = ref activity.Data;

            writer.WriteStartObject();

            if (data.Source != null)
            {
                writer.WriteString("sourceName", data.Source.Name);
                if (data.Source.Version != null)
                {
                    writer.WriteString("sourceVersion", data.Source.Version);
                }
            }
            writer.WriteString("operationName", data.OperationName);
            if (!string.IsNullOrEmpty(data.DisplayName))
            {
                writer.WriteString("displayName", data.DisplayName);
            }
            writer.WriteString("kind", data.Kind.ToString());
            writer.WriteString("traceId", data.TraceId.ToHexString());
            writer.WriteString("spanId", data.SpanId.ToHexString());
            if (data.ParentSpanId != default)
            {
                writer.WriteString("parentSpanId", data.ParentSpanId.ToHexString());
            }
            writer.WriteString("traceFlags", data.TraceFlags.ToString());

            writer.WriteString("startTimeUtc", data.StartTimeUtc);
            writer.WriteString("endTimeUtc", data.EndTimeUtc);

            if (data.Status != ActivityStatusCode.Unset)
            {
                writer.WriteString("status", data.Status.ToString());
                if (!string.IsNullOrEmpty(data.StatusDescription))
                {
                    writer.WriteString("statusDescription", data.StatusDescription);
                }
            }

            writer.WriteStartArray("tags");

            for (int i = 0; i < activity.Tags.Length; i++)
            {
                KeyValuePair<string, object?> tag = activity.Tags[i];

                if (tag.Value != null)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName(tag.Key);
                    SerializeValue(writer, tag.Value);
                    writer.WriteEndObject();
                }
            }

            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        private static void SerializeValue(Utf8JsonWriter writer, object value)
        {
            switch (value)
            {
                case bool b: writer.WriteBooleanValue(b); break;
#if NET8_0_OR_GREATER
                case char c: writer.WriteStringValue(new ReadOnlySpan<char>(ref c)); break;
#elif NET7_0_OR_GREATER
                case char c: writer.WriteStringValue(new ReadOnlySpan<char>(in c)); break;
#else
                case char c:
                    {
                        Span<char> data = stackalloc char[1];
                        data[0] = c;
                        writer.WriteStringValue(data);
                        break;
                    }
#endif
                case byte b: writer.WriteNumberValue(b); break;
                case sbyte b: writer.WriteNumberValue(b); break;
                case short s: writer.WriteNumberValue(s); break;
                case ushort s: writer.WriteNumberValue(s); break;
                case int i: writer.WriteNumberValue(i); break;
                case uint i: writer.WriteNumberValue(i); break;
                case long l: writer.WriteNumberValue(l); break;
                case ulong l: writer.WriteNumberValue(l); break;
                case double d: writer.WriteNumberValue(d); break;
                case float f: writer.WriteNumberValue(f); break;
                case decimal d: writer.WriteNumberValue(d); break;
                case string s: writer.WriteStringValue(s); break;
                case DateTime d: writer.WriteStringValue(d); break;
                case DateTimeOffset dto: writer.WriteStringValue(dto); break;
                case Guid g: writer.WriteStringValue(g); break;
                case byte[] b: writer.WriteBase64StringValue(b); break;
                case Memory<byte> b: writer.WriteBase64StringValue(b.Span); break;
                case ArraySegment<byte> b: writer.WriteBase64StringValue(b); break;

                case TimeSpan t: JsonMetadataServices.TimeSpanConverter.Write(writer, t, null!); break;
                case Uri u: JsonMetadataServices.UriConverter.Write(writer, u, null!); break;
                case Version v: JsonMetadataServices.VersionConverter.Write(writer, v, null!); break;
#if NET7_0_OR_GREATER
                case DateOnly d: JsonMetadataServices.DateOnlyConverter.Write(writer, d, null!); break;
                case TimeOnly t: JsonMetadataServices.TimeOnlyConverter.Write(writer, t, null!); break;
#endif

                case Array v:
                    SerializeArrayValue(writer, v);
                    break;

                case IReadOnlyList<KeyValuePair<string, object>> v:
                    SerializeMapValue(writer, v);
                    break;

                case IEnumerable<KeyValuePair<string, object>> v:
                    SerializeMapValue(writer, v);
                    break;

                default:
                    SerializeObjectValue(writer, value);
                    break;
            }
        }

        private static void SerializeArrayValue(Utf8JsonWriter writer, Array value)
        {
            writer.WriteStartArray();

            foreach (object element in value)
            {
                if (element == null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    SerializeValue(writer, element);
                }
            }

            writer.WriteEndArray();
        }

        private static void SerializeMapValue(Utf8JsonWriter writer, IReadOnlyList<KeyValuePair<string, object>> value)
        {
            writer.WriteStartObject();

            for (int i = 0; i < value.Count; i++)
            {
                KeyValuePair<string, object> element = value[i];

                if (string.IsNullOrEmpty(element.Key))
                {
                    continue;
                }

                writer.WritePropertyName(element.Key);

                if (element.Value == null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    SerializeValue(writer, element.Value);
                }
            }

            writer.WriteEndObject();
        }

        private static void SerializeMapValue(Utf8JsonWriter writer, IEnumerable<KeyValuePair<string, object>> value)
        {
            writer.WriteStartObject();

            foreach (KeyValuePair<string, object> element in value)
            {
                if (string.IsNullOrEmpty(element.Key))
                {
                    continue;
                }

                writer.WritePropertyName(element.Key);

                if (element.Value == null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    SerializeValue(writer, element.Value);
                }
            }

            writer.WriteEndObject();
        }

        private static void SerializeObjectValue(Utf8JsonWriter writer, object value)
        {
            const int MaximumStackAllocSizeInBytes = 256;

            if (value is ISpanFormattable spanFormattable)
            {
                Span<char> destination = stackalloc char[MaximumStackAllocSizeInBytes / 2];
                if (spanFormattable.TryFormat(destination, out int charsWritten, string.Empty, CultureInfo.InvariantCulture))
                {
                    writer.WriteStringValue(destination.Slice(0, charsWritten));
                    return;
                }
            }

            string v;

            try
            {
                v = Convert.ToString(value, CultureInfo.InvariantCulture)!;
            }
            catch
            {
                v = $"ERROR: type {value.GetType().FullName} is not supported";
            }

            writer.WriteStringValue(v);
        }

        private class Activity
        {
            private readonly ActivityData _Data;

            public Activity(in ActivityData data, KeyValuePair<string, object?>[] tags)
            {
                _Data = data;
                Tags = tags;
            }

            public ref readonly ActivityData Data => ref _Data;

            public KeyValuePair<string, object?>[] Tags { get; }
        }
    }
}
