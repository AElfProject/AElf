using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace AElf.Common.MultiIndexDictionary
{
    /// <summary>
    /// Inspired by https://github.com/gnaeus/MultiIndexCollection
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class IndexedDictionary<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
    {
        readonly List<IEqualityIndex<T>> _indexes = new List<IEqualityIndex<T>>(2);
        readonly Dictionary<T, List<object>> _storage;

        public bool IsSynchronized => false;
        public object SyncRoot => ((ICollection) _storage).SyncRoot;
        public int Count => _storage.Count;
        public bool IsReadOnly => false;

        public IndexedDictionary()
        {
            _storage = new Dictionary<T, List<object>>();
        }

        public IndexedDictionary(IEnumerable<T> enumerable)
        {
            EnsureNotNull(enumerable);
            
            _storage = enumerable.ToDictionary(item => item, _ => new List<object>(2));
        }
        
        public IndexedDictionary<T> IndexBy<TProperty>(Expression<Func<T, TProperty>> property, bool isSorted = false)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            if (isSorted)
            {
                IndexBy(new ComparisionIndex<T, TProperty>(property));
            }
            else
            {
                IndexBy(new EqualityIndex<T, TProperty>(property));
            }
            
            return this;
        }
        
        public IndexedDictionary<T> IndexByIgnoreCase(Expression<Func<T, string>> property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            
            IndexBy(new ComparisionIndex<T, string>(property, StringComparer.OrdinalIgnoreCase));

            return this;
        }
        
        private void IndexBy(IEqualityIndex<T> index)
        {
            foreach (var pair in _storage)
            {
                T item = pair.Key;
                List<object> indexKeys = pair.Value;

                object key = index.GetKey(item);

                indexKeys.Add(key);
                index.Add(key, item);
            }

            _indexes.Add(index);
        }

        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        private IComparisionIndex<T> FindComparisionIndex(Expression memberExpression)
        {
            string memberName = memberExpression.GetMemberName();

            var index = (IComparisionIndex<T>)_indexes
                .Find(i => i.MemberName == memberName && i is IComparisionIndex<T>);

            if (index == null)
            {
                throw new InvalidOperationException(
                    $"There is no comparision index for property '{memberName}'");
            }

            return index;
        }
        
        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        private ILookup<TProperty, T> FindLookup<TProperty>(Expression memberExpression)
        {
            string memberName = memberExpression.GetMemberName();
            
            var lookup = (ILookup<TProperty, T>)_indexes
                .Find(i => i.MemberName == memberName && i is ILookup<TProperty, T>);

            if (lookup == null)
            {
                throw new InvalidOperationException($"There is no index for property '{memberName}'");
            }

            return lookup;
        }
        
        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        private ILookup<TProperty, T> FindEqualityLookup<TProperty>(Expression memberExpression)
        {
            string memberName = memberExpression.GetMemberName();

            var lookup = (ILookup<TProperty, T>)_indexes
                .Find(i => i.MemberName == memberName && i is EqualityIndex<T, TProperty>);

            if (lookup == null)
            {
                throw new InvalidOperationException($"There is no equality index for property '{memberName}'");
            }

            return lookup;
        }
        
         #region LINQ Methods

        public IEnumerable<T> AsEnumerable()
        {
            return this;
        }

        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        public T First(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return Filter(predicate.Body).First();
        }

        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        public T FirstOrDefault(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return Filter(predicate.Body).FirstOrDefault();
        }

        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        public ILookup<TProperty, T> GroupBy<TProperty>(Expression<Func<T, TProperty>> property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            return FindLookup<TProperty>(property.Body);
        }

        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        public IEnumerable<TResult> GroupJoin<TOuter, TKey, TResult>(
            IEnumerable<TOuter> outerEnumerable,
            Expression<Func<T, TKey>> innerKeySelector,
            Func<TOuter, TKey> outerKeySelector,
            Func<IEnumerable<T>, TOuter, TResult> resultSelector)
        {
            if (outerEnumerable == null) throw new ArgumentNullException(nameof(outerEnumerable));
            if (innerKeySelector == null) throw new ArgumentNullException(nameof(innerKeySelector));
            if (outerKeySelector == null) throw new ArgumentNullException(nameof(outerKeySelector));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            var lookup = FindEqualityLookup<TKey>(innerKeySelector.Body);

            foreach (var outer in outerEnumerable)
            {
                var innerEnumerable = lookup[outerKeySelector.Invoke(outer)];

                yield return resultSelector.Invoke(innerEnumerable, outer);
            }
        }

        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        public IEnumerable<TResult> Join<TOuter, TKey, TResult>(
            IEnumerable<TOuter> outerEnumerable,
            Expression<Func<T, TKey>> innerKeySelector,
            Func<TOuter, TKey> outerKeySelector,
            Func<T, TOuter, TResult> resultSelector)
        {
            if (outerEnumerable == null) throw new ArgumentNullException(nameof(outerEnumerable));
            if (innerKeySelector == null) throw new ArgumentNullException(nameof(innerKeySelector));
            if (outerKeySelector == null) throw new ArgumentNullException(nameof(outerKeySelector));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            var lookup = FindEqualityLookup<TKey>(innerKeySelector.Body);

            foreach (var outer in outerEnumerable)
            {
                foreach (var inner in lookup[outerKeySelector.Invoke(outer)])
                {
                    yield return resultSelector.Invoke(inner, outer);
                }
            }
        }

        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        public IEnumerable<T> HavingMax<TProperty>(Expression<Func<T, TProperty>> property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            var index = FindComparisionIndex(property.Body);

            return index.HavingMax();
        }

        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        public IEnumerable<T> HavingMin<TProperty>(Expression<Func<T, TProperty>> property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            IComparisionIndex<T> index = FindComparisionIndex(property.Body);

            return index.HavingMin();
        }
        
        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        public T Last(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return Filter(predicate.Body).Last();
        }

        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        public T LastOrDefault(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return Filter(predicate.Body).LastOrDefault();
        }

        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        public long LongCount(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return Filter(predicate.Body).LongCount();
        }

        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        public TProperty Max<TProperty>(Expression<Func<T, TProperty>> property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            var index = FindComparisionIndex(property.Body);

            return (TProperty)index.Max();
        }

        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        public TProperty Min<TProperty>(Expression<Func<T, TProperty>> property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            var index = FindComparisionIndex(property.Body);

            return (TProperty)index.Min();
        }

        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        public IEnumerable<T> OrderBy<TProperty>(Expression<Func<T, TProperty>> property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            return FindComparisionIndex(property.Body);   
        }

        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        public IEnumerable<T> OrderByDescending<TProperty>(Expression<Func<T, TProperty>> property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            return FindComparisionIndex(property.Body).Reverse();
        }
        
        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        public T Single(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return Filter(predicate.Body).Single();
        }

        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        public T SingleOrDefault(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return Filter(predicate.Body).SingleOrDefault();
        }

        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        public ILookup<TProperty, T> ToLookup<TProperty>(Expression<Func<T, TProperty>> property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            return FindLookup<TProperty>(property.Body);
        }

        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        public IEnumerable<T> Where(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return Filter(predicate.Body);
        }

        #endregion

        #region Filtering

        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        private IEnumerable<T> Filter(Expression body)
        {
            if (body is MethodCallExpression methodCall)
            {
                return FilterStringStartsWith(methodCall);
            }

            var binary = body as BinaryExpression;

            if (binary == null)
            {
                throw new NotSupportedException(
                    $"Predicate body {body} should be Binary Expression");
            }

            switch (binary.NodeType)
            {
                case ExpressionType.OrElse:
                    return Filter(binary.Left).Union(Filter(binary.Right));

                case ExpressionType.AndAlso:
                    return FilterRangeOrAndAlso(binary.Left, binary.Right);

                case ExpressionType.Equal:
                    return FilterEquality(binary.Left, binary.Right);

                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                    return FilterComparision(binary.Left, binary.Right, binary.NodeType);

                default:
                    throw new NotSupportedException(
                        $"Predicate body {body} should be Equality or Comparision");
            }
        }

        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        private IEnumerable<T> FilterEquality(Expression memberExpression, Expression keyExpression)
        {
            string memberName = memberExpression.GetMemberName();

            IEqualityIndex<T> index = _indexes.Find(i => i.MemberName == memberName);

            if (index == null)
            {
                throw new InvalidOperationException($"There is no index for property '{memberName}'");
            }

            return index.Filter(keyExpression.GetValue());
        }

        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        private IEnumerable<T> FilterComparision(
            Expression memberExpression, Expression keyExpression, ExpressionType type)
        {
            IComparisionIndex<T> index = FindComparisionIndex(memberExpression);

            object key = keyExpression.GetValue();

            switch (type)
            {
                case ExpressionType.GreaterThan:
                    return index.GreaterThan(key, true);

                case ExpressionType.GreaterThanOrEqual:
                    return index.GreaterThan(key, false);

                case ExpressionType.LessThan:
                    return index.LessThan(key, true);

                case ExpressionType.LessThanOrEqual:
                    return index.LessThan(key, false);

                default:
                    throw new NotSupportedException($"Expression {type} should be Comparision");
            }
        }

        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        private IEnumerable<T> FilterRangeOrAndAlso(Expression left, Expression right)
        {
            if (!(left is BinaryExpression leftOperation))
            {
                throw new NotSupportedException(
                    $"Predicate body {left} should be Binary Expression");
            }

            var rightOperation = right as BinaryExpression;

            if (rightOperation == null)
            {
                throw new NotSupportedException(
                    $"Predicate body {right} should be Binary Expression");
            }

            var leftMemberName = leftOperation.Left.GetMemberName();
            var rightMemberName = rightOperation.Left.GetMemberName();

            if (leftMemberName == rightMemberName)
            {
                var index = (IComparisionIndex<T>)_indexes
                    .Find(i => i.MemberName == leftMemberName && i is IComparisionIndex<T>);

                if (index != null)
                {
                    switch (leftOperation.NodeType)
                    {
                        case ExpressionType.GreaterThan:
                            switch (rightOperation.NodeType)
                            {
                                case ExpressionType.LessThan:
                                    return index.Between(
                                        leftOperation.Right.GetValue(), true,
                                        rightOperation.Right.GetValue(), true);
                                
                                case ExpressionType.LessThanOrEqual:
                                    return index.Between(
                                        leftOperation.Right.GetValue(), true,
                                        rightOperation.Right.GetValue(), false);
                            }
                            break;

                        case ExpressionType.GreaterThanOrEqual:
                            switch (rightOperation.NodeType)
                            {
                                case ExpressionType.LessThan:
                                    return index.Between(
                                        leftOperation.Right.GetValue(), false,
                                        rightOperation.Right.GetValue(), true);

                                case ExpressionType.LessThanOrEqual:
                                    return index.Between(
                                        leftOperation.Right.GetValue(), false,
                                        rightOperation.Right.GetValue(), false);
                            }
                            break;

                        case ExpressionType.LessThan:
                            switch (rightOperation.NodeType)
                            {
                                case ExpressionType.GreaterThan:
                                    return index.Between(
                                        rightOperation.Right.GetValue(), true,
                                        leftOperation.Right.GetValue(), true);

                                case ExpressionType.GreaterThanOrEqual:
                                    return index.Between(
                                        rightOperation.Right.GetValue(), false,
                                        leftOperation.Right.GetValue(), true);
                            }
                            break;

                        case ExpressionType.LessThanOrEqual:
                            switch (rightOperation.NodeType)
                            {
                                case ExpressionType.GreaterThan:
                                    return index.Between(
                                        rightOperation.Right.GetValue(), true,
                                        leftOperation.Right.GetValue(), false);

                                case ExpressionType.GreaterThanOrEqual:
                                    return index.Between(
                                        rightOperation.Right.GetValue(), false,
                                        leftOperation.Right.GetValue(), false);
                            }
                            break;
                    }
                }
            }
            
            return Filter(left).Intersect(Filter(right));
        }

        /// <exception cref="NotSupportedException" />
        /// <exception cref="InvalidOperationException" />
        private IEnumerable<T> FilterStringStartsWith(MethodCallExpression methodCall)
        {
            if (methodCall.Method.DeclaringType == typeof(String) &&
                methodCall.Method.Name == nameof(string.StartsWith))
            {
                IComparisionIndex<T> index = FindComparisionIndex(methodCall.Object);

                string keyFrom = (string)methodCall.Arguments.First().GetValue();

                if (String.IsNullOrEmpty(keyFrom))
                {
                    return index.GreaterThan(keyFrom, false);
                }

                char lastChar = keyFrom[keyFrom.Length - 1];

                string keyTo = keyFrom.Substring(0, keyFrom.Length - 1) + (char)(lastChar + 1);

                return index.Between(keyFrom, false, keyTo, true);
            }

            throw new NotSupportedException(
                $"Predicate body {methodCall} should be String.StartsWith()");
        }

        #endregion

        #region Updating

        /// <summary>
        /// Adds an element to the <see cref="IndexedDictionary{T}"/>.
        /// </summary>
        /// <param name="item"> The element to add to the <see cref="IndexedDictionary{T}"/>. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="item"/> is null. </exception>
        public void Add(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            AddOrUpdate(item);
        }

        /// <summary>
        /// Apply changes in indexed properties of the specified <paramref name="item"/>
        /// to the <see cref="IndexedDictionary{T}"/> indexes.
        /// </summary>
        /// <param name="item"> The element to update. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="item"/> is null. </exception>
        public void Update(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            AddOrUpdate(item);
        }

        private void AddOrUpdate(T item)
        {
            if (_storage.TryGetValue(item, out List<object> indexKeys))
            {
                for (int i = 0; i < _indexes.Count; i++)
                {
                    IEqualityIndex<T> index = _indexes[i];
                    object currentKey = index.GetKey(item);
                    object lastKey = indexKeys[i];

                    if (!Equals(lastKey, currentKey))
                    {
                        indexKeys[i] = currentKey;
                        index.Remove(lastKey, item);
                        index.Add(currentKey, item);
                    }
                }
            }
            else
            {
                indexKeys = new List<object>(_indexes.Count);

                foreach (var index in _indexes)
                {
                    object key = index.GetKey(item);

                    indexKeys.Add(key);
                    index.Add(key, item);
                }

                _storage.Add(item, indexKeys);
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Removes the specified element from a <see cref="T:AElf.Common.MultiIndexDictionary.IndexedDictionary`1" />.
        /// </summary>
        /// <param name="item"> The element to remove. </param>
        /// <returns>
        /// true if the element is successfully found and removed; otherwise, false.
        /// This method returns false if item is not found in the <see cref="T:AElf.Common.MultiIndexDictionary.IndexedDictionary`1" />.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException"> <paramref name="item" /> is null. </exception>
        public bool Remove(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            if (_storage.TryGetValue(item, out var indexKeys))
            {
                for (var i = 0; i < _indexes.Count; i++)
                {
                    var index = _indexes[i];
                    var lastKey = indexKeys[i];

                    index.Remove(lastKey, item);
                }

                _storage.Remove(item);

                return true;
            }

            return false;
        }

        /// <inheritdoc />
        /// <summary>
        /// Removes all items from the <see cref="T:AElf.Common.MultiIndexDictionary.IndexedDictionary`1" />.
        /// </summary>
        public void Clear()
        {
            foreach (var index in _indexes)
            {
                index.Clear();
            }

            _storage.Clear();
        }

        private void AddItems(IEnumerable newItems)
        {
            foreach (var newItem in newItems)
            {
                if (newItem is T item)
                {
                    AddOrUpdate(item);
                }
            }
        }

        private void RemoveItems(IEnumerable oldItems)
        {
            foreach (var oldItem in oldItems)
            {
                if (oldItem is T item)
                {
                    Remove(item);
                }
            }
        }

        private void ResetItems(object sender)
        {
            Clear();

            if (!(sender is IEnumerable<T> items))
                return;
            foreach (var item in items)
            {
                AddOrUpdate(item);
            }
        }

        #endregion

        /// <inheritdoc />
        /// <summary>
        /// Determines whether the <see cref="T:AElf.Common.MultiIndexDictionary.IndexedDictionary`1" /> contains a specific value.
        /// </summary>
        /// <param name="item"> The object to locate in the <see cref="T:AElf.Common.MultiIndexDictionary.IndexedDictionary`1" />. </param>
        /// <returns> true if item is found in the <see cref="T:AElf.Common.MultiIndexDictionary.IndexedDictionary`1" />; otherwise, false. </returns>
        /// <exception cref="T:System.ArgumentNullException"> <paramref name="item" /> is null. </exception>
        public bool Contains(T item)
        {
            return item != null && _storage.ContainsKey(item);
        }

        /// <inheritdoc />
        /// <summary>
        /// Copies the elements of the <see cref="T:AElf.Common.MultiIndexDictionary.IndexedDictionary`1" /> to an <see cref="T:System.Array" />
        /// starting at a particular <see cref="T:System.Array" /> index.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from
        /// <see cref="T:AElf.Common.MultiIndexDictionary.IndexedDictionary`1" />. The <see cref="T:System.Array" /> must have zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException"> <paramref name="array" /> is null. </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"> <paramref name="arrayIndex" /> is less than 0. </exception>
        /// <exception cref="T:System.ArgumentException">
        /// The number of elements in the source <see cref="T:AElf.Common.MultiIndexDictionary.IndexedDictionary`1" /> is greater than
        /// the available space from <paramref name="arrayIndex" /> to the end of the destination <paramref name="array" />.
        /// </exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            _storage.Keys.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<T> GetEnumerator()
        {
            return _storage.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)_storage.Keys).CopyTo(array, index);
        }

        [DebuggerStepThrough]
        private void EnsureNotNull(object obj)
        {
            if (obj == null)
            {
                throw new NullReferenceException(nameof(obj));
            }
        }
    }
}