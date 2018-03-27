using AElf.Kernel.Extensions;
using System;

namespace AElf.Kernel
{
    public class Hash : IHash
    {


        public static Hash Generate()
        {
            return new Hash(
                HashExtensions.CalculateHash(Guid.NewGuid().ToByteArray()));
        }
        
        public static readonly Hash Zero = new Hash();

        public byte[] Value { get; set; }

        public Hash(byte[] buffer)
        {
            Value = buffer;
        } 

        //TODO: define length in a static property
        // ReSharper disable once MemberCanBePrivate.Global
        public Hash() : this(new byte[HashExtensions.Length])
        {

        }

        public override string ToString() => Value.ToHex();

        public byte[] GetHashBytes() => Value;

        public bool Equals(IHash other)
        {
            var bytes = GetHashBytes();
            var otherBytes = other.GetHashBytes();
            if (bytes.Length != otherBytes.Length)
            {
                return false;
            }
            for (var i = 0; i < Math.Min(bytes.Length, otherBytes.Length); i++)
            {
                if (bytes[i] != otherBytes[i])
                {
                    return false;
                }
            }
            return true;
        }

        public int Compare(IHash x, IHash y)
        {
            if (x == y)
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
    }
}
