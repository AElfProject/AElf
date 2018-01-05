using System;
using System.Collections.Generic;
using System.Text;

namespace AElf.Kernel.MerkleTree
{
    public class MerkleException : ApplicationException
    {
        public MerkleException(string msg) : base(msg) { }
    }
}
