using System;
using System.Collections.Generic;
using System.Text;

namespace AElf.Kernel.Merkle
{
    public class MerkleHashCompare : IComparer<MerkleHash>
    {
        public int Compare(MerkleHash x, MerkleHash y)
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
