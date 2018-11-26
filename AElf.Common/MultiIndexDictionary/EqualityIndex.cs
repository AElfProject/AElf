using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AElf.Common.MultiIndexDictionary
{
    public class EqualityIndex<T, TProperty> : Dictionary<TProperty, object>,
        IEqualityIndex<T>, ILookup<TProperty, T>
    {
        public string MemberName { get; }
        
        readonly Func<T, TProperty> _getKey;

        private object _nullBucket;
        
        const int MaxListBucketCount = 16;

        /// <exception cref="NotSupportedException" />
        public EqualityIndex(Expression<Func<T, TProperty>> lambda)
        {
            MemberName = lambda.Body.GetMemberName();

            _getKey = lambda.Body.CreateGetter<T, TProperty>();
        }

        public object GetKey(T item)
        {
            return _getKey.Invoke(item);
        }

        public IEnumerable<T> Filter(object key)
        {
            if (key == null)
            {
                if (_nullBucket != null)
                {
                    return _nullBucket is T element
                        ? new[] { element }
                        : (IEnumerable<T>)_nullBucket;
                }
            }
            else if (TryGetValue((TProperty)key, out object bucket))
            {
                return bucket is T element
                    ? new[] { element }
                    : (IEnumerable<T>)bucket;
            }

            return Enumerable.Empty<T>();
        }

        public void Add(object key, T item)
        {
            if (key == null)
            {
                if (_nullBucket is HashSet<T> hashSet)
                {
                    hashSet.Add(item);
                }
                else if (_nullBucket is List<T> list)
                {
                    if (list.Count < MaxListBucketCount)
                    {
                        list.Add(item);
                    }
                    else
                    {
                        _nullBucket = new HashSet<T>(list) { item };
                    }   
                }
                else if (_nullBucket is T element)
                {
                    _nullBucket = new List<T>(2) { element, item };
                }
                else
                {
                    _nullBucket = item;
                }
                
                return;
            }

            var propKey = (TProperty)key;

            if (TryGetValue(propKey, out object bucket))
            {
                if (bucket is T element)
                {
                    base[propKey] = new List<T>(2) { element, item };
                }
                else if (bucket is List<T> list)
                {
                    if (list.Count < MaxListBucketCount)
                    {
                        list.Add(item);
                    }
                    else
                    {
                        base[propKey] = new HashSet<T>(list) { item };
                    }
                }
                else if (bucket is HashSet<T> hashSet)
                {
                    hashSet.Add(item);
                }
            }
            else
            {
                base.Add(propKey, item);
            }
        }

        public void Remove(object key, T item)
        {
            if (key == null)
            {
                if (_nullBucket is HashSet<T> hashSet)
                {
                    hashSet.Remove(item);

                    if (hashSet.Count == MaxListBucketCount)
                    {
                        _nullBucket = new List<T>(hashSet);
                    }
                }
                else if (_nullBucket is List<T> list)
                {
                    list.Remove(item);

                    if (list.Count == 1)
                    {
                        _nullBucket = list[0];
                    }
                }
                else if (_nullBucket is T)
                {
                    _nullBucket = null;
                }

                return;
            }

            var propKey = (TProperty)key;

            if (TryGetValue(propKey, out object bucket))
            {
                if (bucket is T)
                {
                    Remove(propKey);
                }
                else if (bucket is List<T> list)
                {
                    list.Remove(item);

                    if (list.Count == 1)
                    {
                        base[propKey] = list[0];
                    }
                }
                else if (bucket is HashSet<T> hashSet)
                {
                    hashSet.Remove(item);

                    if (hashSet.Count == MaxListBucketCount)
                    {
                        base[propKey] = new List<T>(hashSet);
                    }
                }
            }
        }

        void IEqualityIndex<T>.Clear()
        {
            Clear();

            _nullBucket = null;
        }

        IEnumerable<T> ILookup<TProperty, T>.this[TProperty key]
        {
            get
            {
                if (key == null)
                {
                    if (_nullBucket != null)
                    {
                        return _nullBucket is T element
                            ? new[] { element }
                            : (IEnumerable<T>)_nullBucket;
                    }
                }
                else if (TryGetValue(key, out object bucket))
                {
                    return bucket is T element
                        ? new[] { element }
                        : (IEnumerable<T>)bucket;
                }

                return Enumerable.Empty<T>();
            }
        }

        bool ILookup<TProperty, T>.Contains(TProperty key)
        {
            return ContainsKey(key);
        }

        IEnumerator<IGrouping<TProperty, T>> IEnumerable<IGrouping<TProperty, T>>.GetEnumerator()
        {
            foreach (var pair in this)
            {
                yield return new BucketGrouping<TProperty, T>(pair);
            }

            if (_nullBucket != null)
            {
                yield return new BucketGrouping<TProperty, T>((TProperty)(object)null, _nullBucket);
            }
        }
    }
}