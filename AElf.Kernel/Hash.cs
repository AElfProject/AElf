using AElf.Kernel.Extensions;
using System;
using System.Data.Common;
using System.Linq;
using Google.Protobuf;

namespace AElf.Kernel
{
    public partial class Hash : IHash
    {
        public static Hash Generate()
        {
            return new Hash(
                HashExtensions.CalculateHash(Guid.NewGuid().ToByteArray()));
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
            return this.value_.Equals(other.Value);
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
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (object.ReferenceEquals(h1, null))
            {
                return object.ReferenceEquals(h2, null);
            }
            return  h1.Equals(h2);
        }

        public static bool operator !=(Hash h1, Hash h2)
        {
            return !(h1 == h2);
        }

    }
}
