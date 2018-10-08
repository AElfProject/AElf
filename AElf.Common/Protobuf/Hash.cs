using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AElf.Common.Extensions;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Common
{
    public partial class Hash : ICustomDiagnosticMessage, IComparable<Hash>
    {
        public string ToDiagnosticString()
        {
            return $@"""{Dumps()}""";
        }

        public static Hash FromBytes(byte[] bytes)
        {
            var hash = new Hash()
            {
                Value = ByteString.CopyFrom(bytes.CalculateHash())
            };
            return hash;
        }

        public static Hash FromString(string str)
        {
            return FromBytes(Encoding.UTF8.GetBytes(str));
        }

        public static Hash FromMessage(IMessage message)
        {
            return FromBytes(message.ToByteArray());
        }

        public static Hash FromTwoHashes(Hash hash1, Hash hash2)
        {
            var hashes = new List<Hash>()
            {
                hash1, hash2
            };
            using (var mm = new MemoryStream())
            using (var stream = new CodedOutputStream(mm))
            {
                foreach (var hash in hashes.OrderBy(x=>x))
                {
                    hash.WriteTo(stream);
                }
                stream.Flush();
                mm.Flush();
                return FromBytes(mm.ToArray());
            }
        }
        
        public static Hash Generate()
        {
            return FromBytes(Guid.NewGuid().ToByteArray());
        }

        public static readonly Hash Zero = Hash.FromBytes(new byte[] { });

        public static readonly Hash Default = Hash.FromString("AElf");

        public static readonly Hash Genesis = Hash.FromString("Genesis");

//        public Hash(byte[] buffer)
//        {
//            Value = ByteString.CopyFrom(buffer);
//            HashType = HashType.General;
//        }
//
//        public Hash(ByteString value)
//        {
//            Value = value;
//            HashType = HashType.General;
//        }
//
        public Hash OfType(HashType hashType)
        {
            var hash = Clone();
            hash.HashType = hashType;
            return hash;
        }
//
//        public Hash OfType(int typeIndex)
//        {
//            var hash = Clone();
//            var hashType = (HashType) typeIndex;
//            hash.HashType = hashType;
//            return hash;
//        }
//
        public byte[] GetHashBytes() => Value.ToByteArray();

//        public bool Equals(IHash other)
//        {
//            return value_.Equals(other.Value);
//        }

//        public int Compare(Hash x, Hash y)
//        {
//            if (x == null || y == null)
//            {
//                throw new InvalidOperationException("Cannot compare hash when hash is null");
//            }
//
//            var xValue = x.Value;
//            var yValue = y.Value;
//            for (var i = 0; i < Math.Min(xValue.Length, yValue.Length); i++)
//            {
//                if (xValue[i] > yValue[i])
//                {
//                    return 1;
//                }
//
//                if (xValue[i] < yValue[i])
//                {
//                    return -1;
//                }
//            }
//
//            return 0;
//        }

//        public static bool operator ==(Hash h1, Hash h2)
//        {
//            return h1?.Equals(h2) ?? ReferenceEquals(h2, null);
//        }
//
//        public static bool operator !=(Hash h1, Hash h2)
//        {
//            return !(h1 == h2);
//        }
//
        public static bool operator <(Hash h1, Hash h2)
        {
            return CompareHash(h1, h2) < 0;
        }

        public static bool operator >(Hash h1, Hash h2)
        {
            return CompareHash(h1, h2) > 0;
        }

        public static int CompareHash(Hash hash1, Hash hash2)
        {
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
            var xValue = x.Value;
            var yValue = y.Value;
            for (var i = 0; i < Math.Min(xValue.Length, yValue.Length); i++)
            {
                if (xValue[i] > yValue[i])
                {
                    return 1;
                }

                if (xValue[i] < yValue[i])
                {
                    return -1;
                }
            }

            return 0;
        }
        public int CompareTo(Hash that)
        {
            return Compare(this, that);
        }
//        
//        public static implicit operator Hash(byte[] value)
//        {
//            return value == null ? Default : new Hash(value);
//        }
//
//        public static implicit operator Hash(ByteString value)
//        {
//            return value == null ? Default : new Hash(value);
//        }
//
        public string Dumps()
        {
            return Value.ToByteArray().ToHex();
        }

        public static Hash Loads(string hex)
        {
            var bytes = ByteArrayHelpers.ByteArrayHelpers.FromHexString(hex);
            if (bytes.Length != 32)
            {
                throw new ArgumentOutOfRangeException(nameof(hex));
            }
            return new Hash()
            {
                Value = ByteString.CopyFrom(bytes)
            };
        }
    }
}