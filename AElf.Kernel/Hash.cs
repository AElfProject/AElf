using AElf.Kernel.Extensions;
using System.Collections.Generic;

namespace AElf.Kernel
{
    public class Hash<T> : IHash<T>, IComparer<Hash<T>>
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

        public int Compare(Hash<T> x, Hash<T> y)
        {
            if (x.ToString() == y.ToString())
                return 0;
            if (x.ToString().CompareTo(y.ToString()) > 0)
                return 1;
            else
                return -1;
        }
    }
}
