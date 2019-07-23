using System.Collections.Generic;
using System.Linq;
using AElf.Types;

namespace AElf
{
    public static class MerkleTreeExtensions
    {
        public static Hash ComputeParentNodeWith(this Hash left, Hash right)
        {
            var res = new[] {left, right}.OrderBy(b => b).Select(h => h.ToByteArray())
                .Aggregate(new byte[0], (rawBytes, bytes) => rawBytes.Concat(bytes).ToArray());

            return Hash.FromRawBytes(res);
        }

        public static Hash ComputeBinaryMerkleTreeRootWithLeafNodes(this IEnumerable<Hash> hashList)
        {
            var treeNodeHashes = GenerateBinaryMerkleTreeNodesWithLeafNodes(hashList.ToList());
            return treeNodeHashes.Any() ? treeNodeHashes.Last() : Hash.Empty;
        }

        public static List<Hash> GenerateBinaryMerkleTreeNodesWithLeafNodes(this IList<Hash> hashList)
        {
            if (!hashList.Any())
            {
                return hashList.ToList();
            }

            if (hashList.Count % 2 == 1)
                hashList.Add(hashList.Last());
            var nodeToAdd = hashList.Count / 2;
            var newAdded = 0;
            var i = 0; 
            while (i < hashList.Count - 1)
            {
                var left = hashList[i++];
                var right = hashList[i++];
                hashList.Add(left.ComputeParentNodeWith(right));
                if (++newAdded != nodeToAdd)
                    continue;

                // complete this row
                if (nodeToAdd % 2 == 1 && nodeToAdd != 1)
                {
                    nodeToAdd++;
                    hashList.Add(hashList.Last());
                }

                // start a new row
                nodeToAdd /= 2;
                newAdded = 0;
            }

            return hashList.ToList();
        }

        public static Hash ComputeBinaryMerkleTreeRootWithPathAndLeafNode(this IList<Hash> hashList, Hash leaf)
        {
            return hashList.Aggregate(leaf, (current, node) => current.ComputeParentNodeWith(node));
        }
    }
}