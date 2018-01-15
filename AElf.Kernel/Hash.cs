using System;

namespace AElf.Kernel
{
    [Serializable]
    public class Hash<T> : IHash<T>
    {
        public byte[] Value { get; set; }

        public Hash(byte[] buffer) => Value = buffer;

        public override string ToString() => BitConverter.ToString(Value).Replace("-", "");

        public byte[] GetHashBytes() => Value;

        public bool Equals(IHash other) => Value == other.GetHashBytes();
    }
}
