using System;
using System.Linq;
using System.Text;

namespace AElf.Kernel
{
    public class Hash
    {
        private byte[] _value;

        public byte[] Value
        {
            get => _value;
            set
            {
                if (value.Length != 32)
                {
                    throw new AELFException("Hash value is invalid.");
                }
                _value = value;
            }
        }

        public Hash() { }

        public Hash(byte[] buffer) => Value = buffer.ComputeHash();

        public Hash(string buffer) => Value = Encoding.UTF8.GetBytes(buffer).ComputeHash();

        public Hash(Hash left, Hash right) => Value = left.Value.Concat(right.Value).ToArray().ComputeHash();

        public override string ToString() => BitConverter.ToString(Value).Replace("-", "");

        public byte[] GetHashBytes() => Value;

        public bool Equals(IHash other) => Value == other.GetHashBytes();
    }
}
