using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Types
{
    public partial class Hash : ICustomDiagnosticMessage, IComparable<Hash>, IEnumerable<byte>
    {
        public static readonly Hash Empty = FromByteArray(Enumerable.Range(0, TypeConsts.HashByteArrayLength)
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
            if (bytes.Length != TypeConsts.HashByteArrayLength)
                throw new ArgumentException("Invalid bytes.", nameof(bytes));

            Value = ByteString.CopyFrom(bytes);
        }

        /// <summary>
        /// Gets the hash from a byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static Hash FromRawBytes(byte[] bytes)
        {
            return new Hash(bytes.ComputeHash());
        }

        /// <summary>
        /// Loads the content value from 32-byte long byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Hash FromByteArray(byte[] bytes)
        {
            if (bytes.Length != TypeConsts.HashByteArrayLength)
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
        public static Hash FromString(string str)
        {
            return FromRawBytes(Encoding.UTF8.GetBytes(str));
        }

        /// <summary>
        /// Gets the hash from a Protobuf Message. Its serialized byte array is used for hash calculation.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Hash FromMessage(IMessage message)
        {
            return FromRawBytes(message.ToByteArray());
        }

        /// <summary>
        /// Creates a hash from two hashes. The serialized byte arrays of the two hashes are concatenated and
        /// used to calculate the hash. 
        /// </summary>
        /// <param name="hash1"></param>
        /// <param name="hash2"></param>
        /// <returns></returns>
        public static Hash FromTwoHashes(Hash hash1, Hash hash2)
        {
            var hashes = new List<Hash>
            {
                hash1, hash2
            };
            using (var mm = new MemoryStream())
            using (var stream = new CodedOutputStream(mm))
            {
                foreach (var hash in hashes.OrderBy(x => x))
                {
                    hash.WriteTo(stream);
                }

                stream.Flush();
                mm.Flush();
                return FromRawBytes(mm.ToArray());
            }
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
            if (Value.Length != TypeConsts.HashByteArrayLength)
                throw new ArgumentException("Invalid bytes.", nameof(Value));

            return Value.ToHex();
        }

        public Int64 ToInt64()
        {
            return BitConverter.ToInt64(
                BitConverter.IsLittleEndian ? Value.Reverse().ToArray() : Value.ToArray(), 0);
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