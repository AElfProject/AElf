using System;
using System.Collections;
using System.Collections.Generic;

namespace AElf.Common.MultiIndexDictionary
{
    /// <summary>
    /// Inspired by https://github.com/gnaeus/MultiIndexCollection
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class IndexedDictionary<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
    {
        readonly List<IEqualityIndex<T>> _indexes;

        readonly Dictionary<T, List<object>> _storage;
        
        private int _count;
        private int _count1;
        private int _count2;

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        int ICollection.Count => _count2;

        public bool IsSynchronized => false;
        public object SyncRoot { get; }

        int ICollection<T>.Count => _count;

        public bool IsReadOnly { get; }

        int IReadOnlyCollection<T>.Count => _count1;
    }
}