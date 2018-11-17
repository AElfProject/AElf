using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using AElf.Common.MultiIndexDictionary;

namespace AElf.Common.Extensions
{
/// <summary>
    /// Extension methods for <see cref="IEnumerable{T}"/>.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Make <see cref="IndexedDictionary{T}"/> from <paramref name="enumerable"/>. It provides
        /// fast indexed search by using <see cref="IndexedDictionary{T}.Where(Expression{Func{T, bool}})"/>.
        /// </summary>
        /// <typeparam name="TSource"> The type of the elements of <paramref name="enumerable"/>. </typeparam>
        /// <typeparam name="TProperty"> The type of the property <paramref name="property"/>. </typeparam>
        /// <param name="enumerable"> The source <see cref="IEnumerable{T}"/>. </param>
        /// <param name="property"> An expression to locate property in collection element. </param>
        /// <param name="isSorted"> Should index be sorted or not. </param>
        /// <returns> A <see cref="IndexedDictionary{T}"/> that contains elements copied from the <paramref name="enumerable"/>. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="enumerable"/> is null. </exception>
        /// <exception cref="ArgumentNullException"> <paramref name="property"/> is null. </exception>
        /// <exception cref="NotSupportedException"> Expression <paramref name="property"/> is not a Member Access. </exception>
        public static IndexedDictionary<TSource> IndexBy<TSource, TProperty>(
            this IEnumerable<TSource> enumerable,
            Expression<Func<TSource, TProperty>> property,
            bool isSorted = false)
        {
            if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));
            if (property == null) throw new ArgumentNullException(nameof(property));

            var collection = enumerable as IndexedDictionary<TSource>
                ?? new IndexedDictionary<TSource>(enumerable);

            return collection.IndexBy(property, isSorted);
        }


        /// <summary>
        /// Make <see cref="IndexedDictionary{T}"/> from <paramref name="enumerable"/>. It provides
        /// fast indexed search by using <see cref="IndexedDictionary{T}.Where(Expression{Func{T, bool}})"/>.
        /// Property index performs a case-insensitive ordinal string comparison.
        /// </summary>
        /// <typeparam name="TSource"> The type of the elements of <paramref name="enumerable"/>. </typeparam>
        /// <param name="enumerable"> The source <see cref="IEnumerable{T}"/>. </param>
        /// <param name="property"> An expression to locate string property in collection element. </param>
        /// <returns> A <see cref="IndexedDictionary{T}"/> that contains elements copied from the <paramref name="enumerable"/>. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="enumerable"/> is null. </exception>
        /// <exception cref="ArgumentNullException"> <paramref name="property"/> is null. </exception>
        /// <exception cref="NotSupportedException"> Expression <paramref name="property"/> is not a Member Access. </exception>
        public static IndexedDictionary<TSource> IndexByIgnoreCase<TSource>(
            this IEnumerable<TSource> enumerable,
            Expression<Func<TSource, string>> property)
        {
            if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));
            if (property == null) throw new ArgumentNullException(nameof(property));

            var collection = enumerable as IndexedDictionary<TSource>
                ?? new IndexedDictionary<TSource>(enumerable);

            return collection.IndexByIgnoreCase(property);
        }
    }
}