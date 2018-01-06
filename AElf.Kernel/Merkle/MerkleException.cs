using System;

namespace AElf.Kernel.Merkle
{
    public class MerkleException : ApplicationException
    {
        public MerkleException(string msg) : base(msg) { }
    }
}
