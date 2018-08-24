using System;
using System.Linq;
using AElf.Cryptography.ECDSA;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class Hash : IHash, ICustomDiagnosticMessage
    {
        public string ToDiagnosticString()
        {
            return $@"""{ToHex()}""";
        }

        public static Hash Generate()
        {
            return new Hash(
                Guid.NewGuid().ToByteArray().CalculateHash());
        }
        
        public Hash ToAccount()
        {
            return GetHashBytes().Take(ECKeyPair.AddressLength).ToArray();
        }
        
        public Hash ToChainId()
        {
            return GetHashBytes().Take(ECKeyPair.AddressLength).ToArray();
        }
        
        public bool CheckPrefix(ByteString prefix ){
            if (prefix.Length > Value.Length)
            {
                return false;
            }

            return !prefix.Where((t, i) => t != Value[i]).Any();
        }

        public static readonly Hash Zero = new Hash("AElf".CalculateHash()).ToAccount();
        
        public static readonly Hash Default = new Hash(new byte[]{});
        
        public static readonly Hash Genesis = new Hash("Genesis".CalculateHash());
        public Hash(byte[] buffer)
        {
            Value = ByteString.CopyFrom(buffer);
        }

        public Hash(ByteString value)
        {
            Value = value;
        }

        public byte[] GetHashBytes() => Value.ToByteArray();

        public bool Equals(IHash other)
        {
            return value_.Equals(other.Value);
        }

        public int Compare(IHash x, IHash y)
        {
            if (x == null || y == null)
            {
                throw new InvalidOperationException("Cannot compare hash when hash is null");
            }
            
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

        public static bool operator ==(Hash h1, Hash h2)
        {
            return h1?.Equals(h2) ?? ReferenceEquals(h2, null);
        }

        public static bool operator !=(Hash h1, Hash h2)
        {
            return !(h1 == h2);
        }
        
        public static implicit operator Hash(byte[] value)
        {
            return value == null ? Default : new Hash(value);
        }
        
        public static implicit operator Hash(ByteString value)
        {
            return value == null ? Default : new Hash(value);
        }

        public string ToHex()
        {
            return GetHashBytes().ToHex();
        }
    }
}
