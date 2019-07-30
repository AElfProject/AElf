using System.Collections.Generic;
using System.Linq;
using AElf.Types;

namespace AElf
{
    public static class BinaryMerkleTreeHelper
    {
        public static Hash ComputeRootWithLeafNodes(IEnumerable<Hash> leafNodes)
        {
            var binaryMerkleTree = BinaryMerkleTree.FromLeafNodes(leafNodes.ToList());
            return binaryMerkleTree.Root;
        }
    }
}