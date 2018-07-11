using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AElf.Common.Collections
{
    public class FixedSizedQueue<T>
    {
        public FixedSizedQueue(int sizeLimit)
        {
            Limit = sizeLimit;
        }
        
        private ConcurrentQueue<T> q = new ConcurrentQueue<T>();
        
        private object lockObject = new object();

        public int Limit { get; private set; }
        
        public List<T> Enqueue(T obj)
        {
            List<T> removed = new List<T>();
            
            q.Enqueue(obj);
            
            lock (lockObject)
            {
                while (q.Count > Limit && q.TryDequeue(out var overflow))
                {
                    removed.Add(overflow);
                }
            }

            return removed;
        }

        public virtual bool Contains(T element)
        {
            return q.Contains(element);
        }
        
        public virtual bool Contains(T element, IEqualityComparer<T> comparer)
        {
            return q.Contains(element, comparer);
        }
    }
}