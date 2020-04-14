using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Types
{
    public partial class Hash : ICustomDiagnosticMessage, IComparable<Hash>, IEnumerable<byte>
    {
        public static readonly Hash Empty = LoadFrom(Enumerable.Range(0, AElfConstants.HashByteArrayLength)
            .Select(x => byte.MinValue).ToArray());

        /// <summary>
        /// Used to override IMessage's default string representation.
        /// </summary>
        /// <returns></returns>
        public string ToDiagnosticString()
        {
            return $@"""{ToHex()}""";
        }

        // Make private to avoid confusion
        private Hash(byte[] bytes)
        {
            if (bytes.Length != AElfConstants.HashByteArrayLength)
                throw new ArgumentException("Invalid bytes.", nameof(bytes));

            Value = ByteString.CopyFrom(bytes);
        }

        /// <summary>
        /// Gets the hash from a byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static Hash ComputeFrom(byte[] bytes)
        {
            return new Hash(bytes.ComputeHash());
        }

        /// <summary>
        /// Loads the content value from 32-byte long byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Hash LoadFrom(byte[] bytes)
        {
            if (bytes.Length != AElfConstants.HashByteArrayLength)
                throw new ArgumentException("Invalid bytes.", nameof(bytes));

            return new Hash
            {
                Value = ByteString.CopyFrom(bytes)
            };
        }

        /// <summary>
        /// Gets the hash from a string encoded in UTF8.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Hash ComputeFrom(string str)
        {
            return ComputeFrom(Encoding.UTF8.GetBytes(str));
        }

        /// <summary>
        /// Gets the hash from int32 value.
        /// </summary>
        /// <param name="intValue"></param>
        /// <returns></returns>
        public static Hash ComputeFrom(int intValue)
        {
            return ComputeFrom(intValue.ToBytes(false));
        }

        /// <summary>
        /// Gets the hash from a Protobuf Message. Its serialized byte array is used for hash calculation.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Hash ComputeFrom(IMessage message)
        {
            return ComputeFrom(message.ToByteArray());
        }

        /// <summary>
        /// Dumps the content value to byte array.
        /// </summary>
        /// <returns></returns>
        public byte[] ToByteArray()
        {
            return Value.ToByteArray();
        }

        /// <summary>
        /// Dumps the content value to hex string.
        /// </summary>
        /// <returns></returns>
        public string ToHex()
        {
            if (Value.Length != AElfConstants.HashByteArrayLength)
                throw new ArgumentException("Invalid bytes.", nameof(Value));

            return Value.ToHex();
        }

        public Int64 ToInt64()
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

        /// <summary>
        /// Gets a new hash from two input hashes from bitwise xor operation.
        /// </summary>
        /// <param name="h1"></param>
        /// <param name="h2"></param>
        /// <returns></returns>
        public static Hash operator ^(Hash h1, Hash h2)
        {
            var newBytes = new byte[AElfConstants.HashByteArrayLength];
            for (var i = 0; i < newBytes.Length; i++)
            {
                newBytes[i] = (byte) (h1.Value[i] ^ h2.Value[i]);
            }

            return ComputeFrom(newBytes);
        }

        public int CompareTo(Hash that)
        {
            if (that == null)
                throw new InvalidOperationException("Cannot compare hash when hash is null");

            return CompareHash(this, that);
        }

        private static int CompareHash(Hash hash1, Hash hash2)
        {
            if (hash1 != null)
            {
                return hash2 == null ? 1 : ByteStringHelper.Compare(hash1.Value, hash2.Value);
            }

            if (hash2 == null)
            {
                return 0;
            }

            return -1;
        }

        public IEnumerator<byte> GetEnumerator()
        {
            return Value.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}