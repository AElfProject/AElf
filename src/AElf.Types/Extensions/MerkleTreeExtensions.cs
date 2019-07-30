using System;
using AElf.Types;

namespace AElf
{
    public static class MerkleTreeExtensions
    {
        public static MerklePath GenerateMerklePath(this BinaryMerkleTree binaryMerkleTree, int index)
        {
            if (binaryMerkleTree.Root == null || index >= binaryMerkleTree.LeafCount)
                throw new InvalidOperationException("Cannot generate merkle path from incomplete binary merkle tree.");
            MerklePath path = new MerklePath();
            int firstInRow = 0;
            int rowcount = binaryMerkleTree.LeafCount;
            while (index < binaryMerkleTree.Nodes.Count - 1)
            {
                Hash neighbor;
                bool isLeftNeighbor;
                if (index % 2 == 0)
                {
                    // add right neighbor node
                    neighbor = binaryMerkleTree.Nodes[index + 1];
                    isLeftNeighbor = false;
                }
                else
                {
                    // add left neighbor node
                    neighbor = binaryMerkleTree.Nodes[index - 1];
                    isLeftNeighbor = true;
                }
                
                path.MerklePathNodes.Add(new MerklePathNode
                {
                    Hash = Hash.FromByteArray(neighbor.ToByteArray()),
                    IsLeftChildNode = isLeftNeighbor
                });
                
                rowcount = rowcount % 2 == 0 ? rowcount : rowcount + 1;
                int shift = (index - firstInRow) / 2;
                firstInRow += rowcount;
                index = firstInRow + shift;
                rowcount /= 2;
            }
            
            return path;
        }
    }
}