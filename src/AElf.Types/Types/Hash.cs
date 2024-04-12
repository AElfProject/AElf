using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Types
{

    public partial class Hash : ICustomDiagnosticMessage, IComparable<Hash>, IEnumerable<byte>
    {
        public static readonly Hash Empty = LoadFromByteArray(Enumerable.Range(0, AElfConstants.HashByteArrayLength)
            .Select(x => byte.MinValue).ToArray());

        public int CompareTo(Hash that)
        {
            if (that == null)
                throw new InvalidOperationException("Cannot compare hash when hash is null");

            return CompareHash(this, that);
        }

        /// <summary>
        ///     Used to override IMessage's default string representation.
        /// </summary>
        /// <returns></returns>
        public string ToDiagnosticString()
        {
            return $@"""{ToHex()}""";
        }

        public IEnumerator<byte> GetEnumerator()
        {
            return Value.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Loads the content value from 32-byte long byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Hash LoadFromByteArray(byte[] bytes)
        {
            if (bytes.Length != AElfConstants.HashByteArrayLength)
                throw new ArgumentException("Invalid bytes.", nameof(bytes));

            return new Hash
            {
                Value = ByteString.CopyFrom(bytes)
            };
        }

        /// <summary>
        ///     Loads the content value represented in base64.
        /// </summary>
        /// <param name="base64"></param>
        /// <returns></returns>
        public static Hash LoadFromBase64(string base64)
        {
            var bytes = Convert.FromBase64String(base64);
            return LoadFromByteArray(bytes);
        }

        /// <summary>
        ///     Loads the content value represented in hex string.
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static Hash LoadFromHex(string hex)
        {
            var bytes = ByteArrayHelper.HexStringToByteArray(hex);
            return LoadFromByteArray(bytes);
        }

        /// <summary>
        ///     Dumps the content value to byte array.
        /// </summary>
        /// <returns></returns>
        public byte[] ToByteArray()
        {
            return Value.ToByteArray();
        }

        /// <summary>
        ///     Converts hash into hexadecimal representation.
        /// </summary>
        /// <returns></returns>
        public string ToHex()
        {
            return Value.ToHex();
        }

        /// <summary>
        ///     Converts hash into int64 value.
        /// </summary>
        /// <returns></returns>
        public long ToInt64()
        {
            return ToByteArray().ToInt64(true);
        }

        public static bool operator ==(Hash h1, Hash h2)
        {
            return h1?.Equals(h2) ?? ReferenceEquals(h2, null);
        }

        public static bool operator !=(Hash h1, Hash h2)
        {
            return !(h1 == h2);
        }

        public static bool operator <(Hash h1, Hash h2)
        {
            return CompareHash(h1, h2) < 0;
        }

        public static bool operator >(Hash h1, Hash h2)
        {
            return CompareHash(h1, h2) > 0;
        }

        private static int CompareHash(Hash hash1, Hash hash2)
        {
            if (hash1 != null) return hash2 == null ? 1 : ByteStringHelper.Compare(hash1.Value, hash2.Value);

            if (hash2 == null) return 0;

            return -1;
        }
    }
}