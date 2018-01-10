using System;
using System.Linq;
using System.Text;

namespace AElf.Kernel
{
    public class Hash<T> : IHash<T>
    {
        public byte[] Value { get; set; }

        public Hash(byte[] buffer) => Value = buffer;

        //public Hash(Hash<T> left, Hash<T> right) => Value = left.Value.Concat(right.Value).ToArray().ComputeHash();

        public override string ToString() => BitConverter.ToString(Value).Replace("-", "");

        public byte[] GetHashBytes() => Value;

        public bool Equals(IHash other) => Value == other.GetHashBytes();
    }
}
