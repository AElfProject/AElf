using System;
using System.Linq;
using System.Text;

namespace AElf.Kernel.Merkle
{
    public class MerkleHash
    {
        private byte[] _value;

        public byte[] Value
        {
            get => _value;
            set
            {
                if (value.Length != 32)
                {
                    throw new MerkleException("Hash value is invalid.");
                }
                _value = value;
            }
        }

        public MerkleHash(byte[] buffer)
        {
            Value = buffer.ComputeHash();
        }

        public MerkleHash(string buffer)
        {
            Value = Encoding.UTF8.GetBytes(buffer).ComputeHash();
        }

        public MerkleHash(MerkleHash left, MerkleHash right)
        {
            Value = left.Value.Concat(right.Value).ToArray().ComputeHash();
        }

        public override string ToString()
        {
            return BitConverter.ToString(Value).Replace("-", "");
        }
    }
}
