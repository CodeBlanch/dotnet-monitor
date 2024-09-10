// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.OpenTelemetry.Resources;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.OpenTelemetry;

internal sealed class BufferedTelemetryBatch<T>
    where T : class, IBufferedTelemetry<T>
{
    private readonly Dictionary<string, LinkedList> _Items = new(StringComparer.OrdinalIgnoreCase);
    private readonly Resource _Resource;

    public BufferedTelemetryBatch(
        Resource resource)
    {
        _Resource = resource ?? throw new ArgumentNullException(nameof(resource));
    }

    public void Add(T item)
    {
        Debug.Assert(item != null);

        ref var linkedList = ref CollectionsMarshal.GetValueRefOrAddDefault(
            _Items, item.Scope.Name, out var exists);

        if (!exists)
        {
            linkedList = new();
        }

        var tail = linkedList!.Tail;

        if (tail == null)
        {
            linkedList.Head = linkedList.Tail = item;
        }
        else
        {
            linkedList.Tail = tail.Next = item;
        }
    }

    public bool WriteTo<TBatchWriter>(
        TBatchWriter writer,
        Action<TBatchWriter, T> writeItemAction,
        CancellationToken cancellationToken)
        where TBatchWriter : IBatchWriter
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.BeginBatch(_Resource);

        foreach (var scope in _Items)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            var linkedList = scope.Value;

            var item = linkedList.Head;
            if (item == null)
            {
                continue;
            }

            writer.BeginInstrumentationScope(item.Scope);

            do
            {
                writeItemAction(writer, item);
            }
            while ((item = item.Next!) != null);

            writer.EndInstrumentationScope();
        }

        writer.EndBatch();

        return true;
    }

    public void Reset()
    {
        foreach (var scope in _Items)
        {
            var linkedList = scope.Value;

            if (linkedList.Head != null)
            {
                linkedList.Head = linkedList.Tail = null;
            }
        }
    }

    private sealed class LinkedList
    {
        public T? Head;

        public T? Tail;
    }
}
