using AElf.Kernel.Extensions;
using System;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class Hash : IHash
    {
        public static Hash Generate()
        {
            return new Hash(
                Guid.NewGuid().ToByteArray().CalculateHash());
        }
        
        public static readonly Hash Zero = new Hash();

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
            if (Equals(x, y))
                return 0;

            var xValue = x.Value;
            var yValue = y.Value;
            for (var i = 0; i < Math.Min(xValue.Length, yValue.Length); i++)
            {
                if (xValue[i] > yValue[i])
                {
                    return 1;
                }
            }

            return -1;
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
            return value == null ? Zero : new Hash(value);
        }
    }
}
