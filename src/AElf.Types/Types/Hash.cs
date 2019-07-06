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
        /// <summary>
        /// Used to override IMessage's default string representation.
        /// </summary>
        /// <returns></returns>
        public string ToDiagnosticString()
        {
            return ToHex();
        }

        // Make private to avoid confusion
        private Hash(byte[] bytes)
        {
            CheckLength(bytes);                        
            Value = ByteString.CopyFrom(bytes.ToArray());
        }

        #region Hashes from various types

        /// <summary>
        /// Gets the hash from a byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static Hash FromRawBytes(byte[] bytes)
        {
            return new Hash(bytes.CalculateHash());
        }

        // TODO: From / Calc / Compute / Load ??
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
            // TODO: XOR??
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

        #endregion

        #region Predefined

        public static readonly Hash Empty = FromByteArray(Enumerable.Range(0, 32).Select(x => byte.MinValue).ToArray());

        #endregion

        #region Comparing

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
            // TODO: why not use Hash.Value??
            if (hash1 != null)
            {
                return hash2 == null ? 1 : Compare(hash1, hash2);
            }

            if (hash2 == null)
            {
                return 0;
            }

            return -1;
        }

        private static int Compare(Hash x, Hash y)
        {
            if (x == null || y == null)
            {
                throw new InvalidOperationException("Cannot compare hash when hash is null");
            }

            return ByteStringHelper.Compare(x.Value, y.Value);
        }

        public int CompareTo(Hash that)
        {
            return Compare(this, that);
        }

        #endregion

        #region Bitwise operations

        /// <summary>
        /// Gets a new hash from two input hashes from bitwise xor operation.
        /// </summary>
        /// <param name="h1"></param>
        /// <param name="h2"></param>
        /// <returns></returns>
        public static Hash Xor(Hash h1, Hash h2)
        {
            var newBytes = new byte[TypeConsts.HashByteArrayLength];
            for (var i = 0; i < newBytes.Length; i++)
            {
                newBytes[i] = (byte) (h1.Value[i] ^ h2.Value[i]);
            }

            return new Hash
            {
                Value = ByteString.CopyFrom(newBytes)
            };
        }

        #endregion

        // TODO: Rename
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
            CheckLength();
            return Value.ToHex();
        }

        public Int64 ToInt64()
        {
            return BitConverter.ToInt64(
                BitConverter.IsLittleEndian ? this.Value.Reverse().ToArray() : this.Value.ToArray(), 0);
        }

        /// <summary>
        /// Loads the content value from 32-byte long byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static Hash FromByteArray(byte[] bytes)
        {
            CheckLength(bytes);
            return new Hash
            {
                Value = ByteString.CopyFrom(bytes)
            };
        }

        public IEnumerator<byte> GetEnumerator()
        {
            return Value.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        private void CheckLength()
        {
            if (Value.Length != TypeConsts.HashByteArrayLength)
            {
                throw new ArgumentOutOfRangeException($"Hash bytes has to be " +
                                                      $"{TypeConsts.HashByteArrayLength} bytes long. The input is {Value.Length} bytes long.");
            }
        }

        private static void CheckLength(byte[] bytes)
        {
            if (bytes.Length != TypeConsts.HashByteArrayLength)
            {
                throw new ArgumentOutOfRangeException($"Hash bytes has to be " +
                                                      $"{TypeConsts.HashByteArrayLength} bytes long. The input is {bytes.Length} bytes long.");
            }
        }
    }
}