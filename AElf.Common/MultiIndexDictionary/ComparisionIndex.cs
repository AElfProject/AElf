using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AElf.Common.MultiIndexDictionary
{
    public class ComparisionIndex<T, TProperty> : SortedSet<KeyValuePair<TProperty, object>>,
        IComparisionIndex<T>, ILookup<TProperty, T>
    {
        public string MemberName { get; }

        readonly Func<T, TProperty> _getKey;

        private object _nullBucket;

        private const int MaxListBucketCount = 16;

        /// <exception cref="NotSupportedException" />
        private ComparisionIndex(Expression<Func<T, TProperty>> lambda, KeyValueComparer comparer)
            : base(comparer)
        {
            MemberName = lambda.Body.GetMemberName();

            _getKey = lambda.Body.CreateGetter<T, TProperty>();
        }

        /// <exception cref="NotSupportedException" />
        public ComparisionIndex(Expression<Func<T, TProperty>> lambda)
            : this(lambda, DefaultComparer)
        {
        }

        /// <exception cref="NotSupportedException" />
        public ComparisionIndex(Expression<Func<T, TProperty>> lambda, IComparer<TProperty> comparer)
            : this(lambda, new KeyValueComparer(comparer))
        {
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
                        ? new[] {element}
                        : (IEnumerable<T>) _nullBucket;
                }
            }
            else
            {
                var pairKey = new KeyValuePair<TProperty, object>((TProperty) key, null);

                object bucket = GetViewBetween(pairKey, pairKey).FirstOrDefault().Value;

                if (bucket != null)
                {
                    return bucket is T element
                        ? new[] {element}
                        : (IEnumerable<T>) bucket;
                }
            }

            return Enumerable.Empty<T>();
        }

        public IEnumerable<T> GreaterThan(object key, bool exclusive)
        {
            return Between(key, exclusive, Max.Key, false);
        }

        public IEnumerable<T> LessThan(object key, bool exclusive)
        {
            return Between(Min.Key, false, key, exclusive);
        }

        public IEnumerable<T> Between(object keyFrom, bool excludeFrom, object keyTo, bool excludeTo)
        {
            var pairFrom = new KeyValuePair<TProperty, object>((TProperty) keyFrom, null);
            var pairTo = new KeyValuePair<TProperty, object>((TProperty) keyTo, null);

            IEnumerable<KeyValuePair<TProperty, object>> range = GetViewBetween(pairFrom, pairTo);

            if (excludeFrom)
            {
                range = range.SkipWhile(pair => Comparer.Compare(pair, pairFrom) == 0);
            }

            if (excludeTo)
            {
                range = range.TakeWhile(pair => Comparer.Compare(pair, pairTo) < 0);
            }

            foreach (var pair in range)
            {
                if (pair.Value is T element)
                {
                    yield return element;
                }
                else
                {
                    foreach (T item in (IEnumerable<T>) pair.Value)
                    {
                        yield return item;
                    }
                }
            }
        }

        public IEnumerable<T> HavingMin()
        {
            object bucket = Min.Value;

            if (bucket != null)
            {
                return bucket is T element
                    ? new[] {element}
                    : (IEnumerable<T>) bucket;
            }

            return Enumerable.Empty<T>();
        }

        public IEnumerable<T> HavingMax()
        {
            object bucket = Max.Value;

            if (bucket != null)
            {
                return bucket is T element
                    ? new[] {element}
                    : (IEnumerable<T>) bucket;
            }

            return Enumerable.Empty<T>();
        }

        /// <exception cref="InvalidOperationException" />
        object IComparisionIndex<T>.Min()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("Sequence contains no elements");
            }

            return Min.Key;
        }

        /// <exception cref="InvalidOperationException" />
        object IComparisionIndex<T>.Max()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("Sequence contains no elements");
            }

            return Max.Key;
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
                        _nullBucket = new HashSet<T>(list) {item};
                    }
                }
                else if (_nullBucket is T element)
                {
                    _nullBucket = new List<T>(2) {element, item};
                }
                else
                {
                    _nullBucket = item;
                }

                return;
            }

            var propKey = (TProperty) key;

            var pairKey = new KeyValuePair<TProperty, object>(propKey, null);

            if (Contains(pairKey))
            {
                object bucket = GetViewBetween(pairKey, pairKey).FirstOrDefault().Value;

                if (bucket is T element)
                {
                    Remove(pairKey);
                    Add(new KeyValuePair<TProperty, object>(propKey, new List<T>(2) {element, item}));
                }
                else if (bucket is List<T> list)
                {
                    if (list.Count < MaxListBucketCount)
                    {
                        list.Add(item);
                    }
                    else
                    {
                        Remove(pairKey);
                        Add(new KeyValuePair<TProperty, object>(propKey, new HashSet<T>(list) {item}));
                    }
                }
                else if (bucket is HashSet<T> hashSet)
                {
                    hashSet.Add(item);
                }
            }
            else
            {
                Add(new KeyValuePair<TProperty, object>(propKey, item));
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

            var propKey = (TProperty) key;

            var pairKey = new KeyValuePair<TProperty, object>(propKey, null);

            if (Contains(pairKey))
            {
                object bucket = GetViewBetween(pairKey, pairKey).FirstOrDefault().Value;

                if (bucket is T)
                {
                    Remove(pairKey);
                }
                else if (bucket is List<T> list)
                {
                    list.Remove(item);

                    if (list.Count == 1)
                    {
                        Remove(pairKey);
                        Add(new KeyValuePair<TProperty, object>(propKey, list[0]));
                    }
                }
                else if (bucket is HashSet<T> hashSet)
                {
                    hashSet.Remove(item);

                    if (hashSet.Count == MaxListBucketCount)
                    {
                        Remove(pairKey);
                        Add(new KeyValuePair<TProperty, object>(propKey, new List<T>(hashSet)));
                    }
                }
            }
        }

        void IEqualityIndex<T>.Clear()
        {
            Clear();

            _nullBucket = null;
        }

        IEnumerable<T> IComparisionIndex<T>.Reverse()
        {
            foreach (var pair in Reverse())
            {
                if (pair.Value is T element)
                {
                    yield return element;
                }
                else
                {
                    foreach (T item in (IEnumerable<T>) pair.Value)
                    {
                        yield return item;
                    }
                }
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            foreach (var pair in this)
            {
                if (pair.Value is T element)
                {
                    yield return element;
                }
                else
                {
                    foreach (T item in (IEnumerable<T>) pair.Value)
                    {
                        yield return item;
                    }
                }
            }
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
                            ? new[] {element}
                            : (IEnumerable<T>) _nullBucket;
                    }
                }
                else
                {
                    var pairKey = new KeyValuePair<TProperty, object>(key, null);

                    object bucket = GetViewBetween(pairKey, pairKey).FirstOrDefault().Value;

                    if (bucket != null)
                    {
                        return bucket is T element
                            ? new[] {element}
                            : (IEnumerable<T>) bucket;
                    }
                }

                return Enumerable.Empty<T>();
            }
        }

        bool ILookup<TProperty, T>.Contains(TProperty key)
        {
            return Contains(new KeyValuePair<TProperty, object>(key, null));
        }

        IEnumerator<IGrouping<TProperty, T>> IEnumerable<IGrouping<TProperty, T>>.GetEnumerator()
        {
            foreach (var pair in this)
            {
                yield return new BucketGrouping<TProperty, T>(pair);
            }

            if (_nullBucket != null)
            {
                yield return new BucketGrouping<TProperty, T>((TProperty) (object) null, _nullBucket);
            }
        }

        static readonly KeyValueComparer DefaultComparer = new KeyValueComparer(Comparer<TProperty>.Default);

        internal class KeyValueComparer : IComparer<KeyValuePair<TProperty, object>>
        {
            readonly IComparer<TProperty> _keyComparer;

            public KeyValueComparer(IComparer<TProperty> keyComparer)
            {
                _keyComparer = keyComparer;
            }

            public int Compare(KeyValuePair<TProperty, object> x, KeyValuePair<TProperty, object> y)
            {
                return _keyComparer.Compare(x.Key, y.Key);
            }
        }
    }
}