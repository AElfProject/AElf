using System;
using AElf.Kernel.Extensions;

namespace AElf.Kernel
{
    [Serializable]
    public class Hash<T> : IHash<T>
    {
        public static readonly Hash<T> Zero = new Hash<T>();
        
        public byte[] Value { get; set; }

        public Hash(byte[] buffer) => Value = buffer;

        //TODO: define length in a static property
        // ReSharper disable once MemberCanBePrivate.Global
        public Hash():this(new byte[32])
        {
           
        }

        public override string ToString() => Value.ToHex();

        public byte[] GetHashBytes() => Value;

        public bool Equals(IHash other) => Value == other.GetHashBytes();
    }
}
