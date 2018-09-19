using System;
using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel.Types.Merkle
{
    /// <summary>
    /// This implementation of binary merkle tree only add hash values as its leaves.
    /// </summary>
    public class BinaryMerkleTree
    {
        /// <summary>
        /// Merkle nodes
        /// </summary>
        private List<Hash> Nodes { get; set; } = new List<Hash>();
        
        /// <summary>
        /// Use a cache to speed up the calculation of hash value.
        /// </summary>
        private readonly Dictionary<string, Hash> _cache = new Dictionary<string, Hash>();

        /// <summary>
        /// Add a leaf node and compute root hash.
        /// </summary>
        /// <param name="hash"></param>
        public void AddNode(Hash hash)
        {
            Nodes.Add(hash);
            ComputeRootHash();
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

        public Hash ComputeRootHash() => ComputeRootHash(Nodes);

        private Hash ComputeRootHash(IList<Hash> hashes)
        {
            while (true)
            {
                if (hashes.Count == 0)
                {
                    return Hash.Default;
                }
                if (hashes.Count == 1) //Finally
                {
                    return new Hash(hashes[0].GetHashBytes());
                }
                //Every time goes to a higher level.
                var parents = new List<Hash>();

                for (var i = 0; i < hashes.Count; i += 2)
                {
                    var right = i + 1 < hashes.Count ? new Hash(hashes[i + 1].Value) : null;
                    var parent = FindCache(hashes[i], right);

                    parents.Add(parent);
                }

                hashes = parents;
            }
        }

        /// <summary>
        /// Find the order of a leaf,
        /// return -1 if the leaf not exists.
        /// </summary>
        /// <param name="leaf"></param>
        /// <returns></returns>
        public int FindLeaf(Hash leaf)
        {
            if (leaf == null)
            {
                return -1;
            }
            for (var i = 0; i < Nodes.Count; i++)
            {
                if (Nodes[i].Equals(leaf))
                {
                    return i;
                }
            }
            return -1;
        }

        public bool VerifyProofList(List<Hash> hashlist)
        {
            var list = ComputeProofHash(hashlist);
            return ComputeRootHash().ToString() == list[0].ToString();
        }

        private List<Hash> ComputeProofHash(List<Hash> hashlist)
        {
            while (true)
            {
                if (hashlist.Count < 2) return hashlist;

                var list = new List<Hash>
                {
                    FindCache(hashlist[0], hashlist[1])
                };

                if (hashlist.Count > 2) 
                    hashlist.GetRange(2, hashlist.Count - 2).ForEach(h => list.Add(h));

                hashlist = list;
            }
        }

        public void UpdateNode(Hash oldLeaf, Hash newLeaf)
        {
            var order = FindLeaf(oldLeaf);
            if (order == -1)
            {
                return;
            }
            UpdateNode(order, newLeaf);
        }

        public void UpdateNode(int oldLeafOrder, Hash newLeaf)
        {
            Nodes[oldLeafOrder] = newLeaf;
            ComputeRootHash();
        }

        private Hash FindCache(Hash hash1, Hash hash2)
        {
            var combineHash = 
                hash2?.Value != null ? 
                    hash1.ToHex() + hash2.ToHex() : 
                    hash1.ToHex();

            return _cache.TryGetValue(combineHash, out var resultHash)
                ? resultHash
                : AddCache(combineHash, new Hash(hash1.CalculateHashWith(hash2)));
        }

        private Hash AddCache(string keyHash, Hash valueHash)
        {
            return _cache[keyHash] = valueHash;
        }
        
        public static int CompareHash(Hash hash1, Hash hash2)
        {
            if (hash1 != null)
            {
                if (hash2 == null)
                {
                    return 1;
                }

                return Compare(hash1, hash2);
            }
            
            if (hash2 == null)
            {
                return 0;
            }
            
            return -1;
        }
        
        private static int Compare(Hash x, Hash y)
        {
            var xValue = x.Value;
            var yValue = y.Value;
            for (var i = 0; i < Math.Min(xValue.Length, yValue.Length); i++)
            {
                if (xValue[i] > yValue[i])
                {
                    return 1;
                }

                if (xValue[i] < yValue[i])
                {
                    return -1;
                }
            }

            return 0;
        }
    }
    
}
