using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace AElf.Common.Collections
{
    public class BlockingPriorityQueue<T>
    {
        public const int DefaultPriorityCountLevels = 2;
        public int PriorityLevels { get; private set; } = DefaultPriorityCountLevels;

        private BlockingCollection<KeyValuePair<int, T>> _itemsCollection;

        public BlockingPriorityQueue()
        {
            var priorityQueue = new PriorityQueue<int, T>(PriorityLevels);
            _itemsCollection = new BlockingCollection<KeyValuePair<int, T>>(priorityQueue);
        }

        public void Enqueue(T obj, int priority)
        {
            if (priority < 0 || priority >= PriorityLevels)
                throw new InvalidOperationException("Priority level out of bounds.");
                
            _itemsCollection.Add(new KeyValuePair<int, T>(priority, obj));
        }

        public T TryTake()
        {
            _itemsCollection.TryTake(out KeyValuePair<int, T> elem);
            return elem.Value;
        }

        public T Take()
        {
            var elem = _itemsCollection.Take();
            return elem.Value;
        }
    }
}