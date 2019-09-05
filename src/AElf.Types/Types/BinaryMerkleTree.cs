using System;
using System.Collections.Generic;
using System.Linq;

namespace AElf.Types
{
    public partial class BinaryMerkleTree
    {
        public static BinaryMerkleTree FromLeafNodes(IEnumerable<Hash> leafNodes)
        {
            var binaryMerkleTree = new BinaryMerkleTree();
            binaryMerkleTree.Nodes.AddRange(leafNodes);
            binaryMerkleTree.LeafCount = binaryMerkleTree.Nodes.Count;
            GenerateBinaryMerkleTreeNodesWithLeafNodes(binaryMerkleTree.Nodes);
            binaryMerkleTree.Root = binaryMerkleTree.Nodes.Any() ? binaryMerkleTree.Nodes.Last() : Hash.Empty;
            return binaryMerkleTree;
        }
        
        /// <summary>
        /// Calculate merkle tree root from leaf node list.
        /// </summary>
        /// <example>
        /// For leave {0,1,2,3,4}, the tree should be like
        ///          12
        ///     10 ------ 11
        ///   6 -- 7    8 -- 9   
        ///  0-1  2-3  4-5
        /// in which {5,9} are copied from {4,8}, and {6,7,8,10,11,12} are calculated.
        /// [12] is merkle tree root.
        /// </example>
        private static void GenerateBinaryMerkleTreeNodesWithLeafNodes(IList<Hash> leafNodes)
        {
            if (!leafNodes.Any())
            {
                return;
            }

            if (leafNodes.Count % 2 == 1)
                leafNodes.Add(leafNodes.Last());
            var nodeToAdd = leafNodes.Count / 2;
            var newAdded = 0;
            var i = 0; 
            while (i < leafNodes.Count - 1)
            {
                var left = leafNodes[i++];
                var right = leafNodes[i++];
                leafNodes.Add(left.Concat(right));
                if (++newAdded != nodeToAdd)
                    continue;

                // complete this row
                if (nodeToAdd % 2 == 1 && nodeToAdd != 1)
                {
                    nodeToAdd++;
                    leafNodes.Add(leafNodes.Last());
                }

                // start a new row
                nodeToAdd /= 2;
                newAdded = 0;
            }
        }

        public MerklePath GenerateMerklePath(int index)
        {
            if (Root == null || index >= LeafCount)
                throw new InvalidOperationException("Cannot generate merkle path from incomplete binary merkle tree.");
            MerklePath path = new MerklePath();
            int indexOfFirstNodeInRow = 0;
            int nodeCountInRow = LeafCount;
            while (index < Nodes.Count - 1)
            {
                Hash neighbor;
                bool isLeftNeighbor;
                if (index % 2 == 0)
                {
                    // add right neighbor node
                    neighbor = Nodes[index + 1];
                    isLeftNeighbor = false;
                }
                else
                {
                    // add left neighbor node
                    neighbor = Nodes[index - 1];
                    isLeftNeighbor = true;
                }
                
                path.MerklePathNodes.Add(new MerklePathNode
                {
                    Hash = Hash.FromByteArray(neighbor.ToByteArray()),
                    IsLeftChildNode = isLeftNeighbor
                });
                
                nodeCountInRow = nodeCountInRow % 2 == 0 ? nodeCountInRow : nodeCountInRow + 1;
                int shift = (index - indexOfFirstNodeInRow) / 2;
                indexOfFirstNodeInRow += nodeCountInRow;
                index = indexOfFirstNodeInRow + shift;
                nodeCountInRow /= 2;
            }
            
            return path;
        }
    }
}