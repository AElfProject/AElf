using System.Linq;
using AElf.Types;

namespace AElf
{
    public static class MerklePathExtensions
    {
        public static Hash ComputeRootWithLeafNode(this MerklePath path, Hash leaf)
        {
            return path.MerklePathNodes.Aggregate(leaf, (current, node) => node.IsLeftChildNode
                ? node.Hash.ComputeWith(current)
                : current.ComputeWith(node.Hash));
        }
    }
}