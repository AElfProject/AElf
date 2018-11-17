using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AElf.Common.MultiIndexDictionary
{
    public class BucketGrouping<TProperty, T> : IGrouping<TProperty, T>
    {
        public TProperty Key { get; }

        readonly object _bucket;

        public BucketGrouping(TProperty key, object bucket)
        {
            Key = key;
            _bucket = bucket;
        }

        public BucketGrouping(KeyValuePair<TProperty, object> pair)
            : this(pair.Key, pair.Value)
        {
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (_bucket is T element)
            {
                yield return element;
            }
            else
            {
                foreach (T item in (IEnumerable<T>)_bucket)
                {
                    yield return item;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        public override bool Equals(object obj)
        {
            return obj is BucketGrouping<TProperty, T> other
                   && Equals(Key, other.Key)
                   && ReferenceEquals(_bucket, other._bucket);
        }

        public override int GetHashCode()
        {
            int hashCode = _bucket.GetHashCode();

            if (Key != null)
            {
                hashCode ^= Key.GetHashCode();
            }

            return hashCode;
        }
    }
}