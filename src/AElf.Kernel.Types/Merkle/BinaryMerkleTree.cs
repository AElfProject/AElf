using System.Collections.Generic;
using System.Linq;
using AElf.Types;

namespace AElf.Kernel
{
    /// <summary>
    /// This implementation of binary merkle tree only add hash values as its leaves.
    /// </summary>
    public partial class BinaryMerkleTree
    {
        /// <summary>
        /// Add a leaf node and compute root hash.
        /// </summary>
        /// <param name="hash"></param>
        public bool AddNode(Hash hash)
        {
            if (Root == null)
                Nodes.Add(hash);
            return Root == null;
        }

        public BinaryMerkleTree AddNodes(IEnumerable<Hash> hashes)
        {
            Nodes.AddRange(hashes);
            return this;
        }

        /// <summary>
        /// Calculate <see cref="Root"/> from leaf node list and set it.
        /// </summary>
        /// <returns>
        /// <see cref="Root"/>
        /// </returns>
        /// <example>
        /// For leave {0,1,2,3,4}, the tree should be like
        ///          12
        ///     10 ------ 11
        ///   6 -- 7    8 -- 9   
        ///  0-1  2-3  4-5
        /// in which {5,9} are copied from {4,8}, and {6,7,8,10,11,12} are calculated.
        /// [12] is <see cref="Root"/>.
        /// </example>
        public Hash ComputeRootHash()
        {
            if (Root != null)
                return Root;
            LeafCount = Nodes.Count;
            Nodes.GenerateBinaryMerkleTreeNodesWithLeafNodes();
            Root = Nodes.Any() ? Nodes.Last() : Hash.Empty;
            return Root;
        }

        /// <summary>
        /// Get merkle proof path for one node.
        /// </summary>
        /// <param name="index">Should be less than <see cref="LeafCount"/>.</param>
        /// <returns>
        /// Return null if <see cref="Root"/> is not calculated or index is not leaf node.
        /// </returns>
        /// <example>
        /// In the tree like
        ///          12
        ///     10 ------ 11
        ///   6 -- 7    8 -- 9   
        ///  0-1  2-3  4-5
        /// For leaf [4], the returned path is {5, 9, 10}.
        /// </example>
        public MerklePath GenerateMerklePath(int index)
        {
            if (Root == null || index >= LeafCount)
                return null;
            MerklePath path = new MerklePath();
            int firstInRow = 0;
            int rowcount = LeafCount;
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
                    Hash = Hash.LoadByteArray(neighbor.DumpByteArray()),
                    IsLeftChildNode = isLeftNeighbor
                });
//                path.Add(index % 2 == 0 ? Nodes[index + 1].Clone() : Nodes[index - 1].Clone());
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