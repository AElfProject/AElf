using System;
using System.Collections.Generic;
using System.Text;

namespace AElf.Kernel
{
    public class MerkleHashCompare : IComparer<Hash<IMerkleNode>>
    {
        public int Compare(Hash<IMerkleNode> x, Hash<IMerkleNode> y)
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