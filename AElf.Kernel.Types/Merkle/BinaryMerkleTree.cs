using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using Google.Protobuf;

namespace AElf.Kernel
{
    /// <summary>
    /// This implementation of binary merkle tree only add hash values as its leaves.
    /// </summary>
    public partial class BinaryMerkleTree
    {
        /// <summary>
        /// Use a cache to speed up the calculation of hash value.
        /// </summary>
        private readonly Dictionary<string, Hash> _cache = new Dictionary<string, Hash>();

        /// <summary>
        /// Add a leaf node and compute root hash.
        /// </summary>
        /// <param name="hash"></param>
        public bool AddNode(Hash hash)
        {
            if(Root ==null)
                Nodes.Add(hash);
            return Root == null;
        }

        public BinaryMerkleTree AddNodes(IEnumerable<Hash> hashes)
        {
            var enumerable = hashes as Hash[] ?? hashes.ToArray();
            var hashesList = enumerable.ToList();
            
            // remove sort here
            //hashesList.Sort(CompareHash);
            
            foreach (var hash in hashesList)
            {
                Nodes.Add(hash);
            }
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
            if (Nodes.Count == 0)
            {
                Root = Hash.Empty;
                return Root;
            }
            LeafCount = Nodes.Count;
            if(Nodes.Count % 2 == 1)
                Nodes.Add(Nodes.Last());
            var nodeToAdd = Nodes.Count / 2;
            var newAdded = 0;
            var i = 0;
            while (i < Nodes.Count-1)
            {
                var left = Nodes[i++];
                var right = Nodes[i++];
                Nodes.Add(CalculateRootFromMultiHash(new []{left, right}));
                if (++newAdded != nodeToAdd) 
                    continue;
                
                // complete this row
                if (nodeToAdd % 2 == 1 && nodeToAdd != 1)
                {
                    nodeToAdd++;
                    Nodes.Add(Nodes.Last());
                }
                // start a new row
                nodeToAdd /= 2;
                newAdded = 0;
            }

            Root = Nodes.Last();
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
        /// For leaf [4], the returned <see cref="MerklePath.Path"/> is {5, 9, 10}.
        /// </example>
        public MerklePath GenerateMerklePath(int index)
        {
            if (Root == null || index >= LeafCount)
                return null;
            List<Hash> path = new List<Hash>();
            int firstInRow = 0;
            int rowcount = LeafCount;
            while (index < Nodes.Count - 1)
            {
                path.Add(index % 2 == 0 ? Nodes[index + 1].Clone() : Nodes[index - 1].Clone());
                rowcount = rowcount % 2 == 0 ? rowcount : rowcount + 1;
                int shift = (index - firstInRow) / 2;
                firstInRow += rowcount;
                index = firstInRow + shift;
                rowcount /= 2;
            }
            var res = new MerklePath();
            res.Path.AddRange(path);
            return res;
        }

        public static Hash CalculateRootFromMultiHash(IEnumerable<Hash> hashList)
        {
            var res = hashList.OrderBy(b => b).Select(h => h.DumpByteArray())
                .Aggregate(new byte[0], (rawBytes, bytes) => rawBytes.Concat(bytes).ToArray());
            
            return Hash.FromRawBytes(res);
        }
    }
}
