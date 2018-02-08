using AElf.Kernel.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace AElf.Kernel.Merkle
{
    public class BinaryMerkleTree<T> : IMerkleTree<T>
    {
        /// <summary>
        /// Merkle nodes
        /// </summary>
        private ConcurrentStack<IHash<T>> Nodes { get; set; } = new ConcurrentStack<IHash<T>>();
        
        private ConcurrentDictionary<string, IHash<T>> _cache = new ConcurrentDictionary<string, IHash<T>>();

        /// <summary>
        /// Add a leaf node and compute root hash.
        /// </summary>
        /// <param name="hash"></param>
        public void AddNode(IHash<T> hash)
        {
            Nodes.Push(hash);
            ComputeRootHash();
        }

        public void AddNode(IHash<T> newHash, IHash<T> oldHash)
        {
            var oldOrder = FindLeaf(oldHash);
            if (oldOrder != -1)
            {
                UpdateNode(oldOrder, newHash);
            }
            else
            {
                AddNode(newHash);
            }
        }

        public BinaryMerkleTree<T> AddNodes(List<IHash<T>> hashes)
        {
            hashes.ForEach(hash => Nodes.Push(hash));

            return this;
        }

        public IHash<IMerkleTree<T>> ComputeRootHash() => ComputeRootHash(Nodes);

        private IHash<IMerkleTree<T>> ComputeRootHash(ConcurrentStack<IHash<T>> hashesbag)
        {
            //Just work around to use a list in this method.
            var hashArray = new IHash<T>[hashesbag.Count];
            hashesbag.CopyTo(hashArray, 0);
            var hashes = hashArray.Reverse().ToList();
            
            if (hashes.Count < 1)
            {
                throw new InvalidOperationException("Cannot generate merkle tree without any nodes.");
            }

            if (hashes.Count == 1)//Finally
            {
                return new Hash<IMerkleTree<T>>(hashes[0].Value);
            }
            else
            {
                //Every time goes to a higher level.
                ConcurrentStack<IHash<T>> parents = new ConcurrentStack<IHash<T>>();

                for (int i = 0; i < hashes.Count; i += 2)
                {
                    IHash<T> right = (i + 1 < hashes.Count) ? new Hash<T>(hashes[i + 1].Value) : null;
                    IHash<T> parent = FindCache(hashes[i], right);

                    parents.Push(parent);
                }

                return ComputeRootHash(parents);
            }
        }

        /// <summary>
        /// Find the order of a leaf,
        /// return -1 if the leaf not exists.
        /// </summary>
        /// <param name="leaf"></param>
        /// <returns></returns>
        public int FindLeaf(IHash<T> leaf)
        {
            if (leaf == null)
            {
                return -1;
            }
            for (int i = 0; i < Nodes.Count; i++)
            {
                Nodes.TryPeek(out var node);
                if (node == leaf)
                {
                    return i;
                }
            }
            return -1;
        }

        public bool VerifyProofList(List<IHash<T>> hashlist)
        {
            List<IHash<T>> list = ComputeProofHash(hashlist);
            return ComputeRootHash().ToString() == list[0].ToString();
        }

        private List<IHash<T>> ComputeProofHash(List<IHash<T>> hashlist)
        {            
            if (hashlist.Count < 2)
                return hashlist;

            List<IHash<T>> list = new List<IHash<T>>()
            {
                new Hash<T>(hashlist[0].CalculateHashWith(hashlist[1]))
            };

            if (hashlist.Count > 2)
                hashlist.GetRange(2, hashlist.Count - 2).ForEach(h => list.Add(h));

            return ComputeProofHash(list);
        }

        public void UpdateNode(IHash<T> oldLeaf, IHash<T> newLeaf)
        {
            int order = FindLeaf(oldLeaf);
            if (order == -1)
            {
                return;
            }
            UpdateNode(order, newLeaf);
        }

        public void UpdateNode(int oldLeafOrder, IHash<T> newLeaf)
        {
            Queue<IHash<T>> temp = new Queue<IHash<T>>();
            for (var i = 0; i < Nodes.Count - oldLeafOrder + 1; i++)
            {
                if (Nodes.TryPop(out var element))
                {
                    temp.Enqueue(element);
                }
            }
            Nodes.Push(newLeaf);
            for (int i = 0; i < temp.Count; i++)
            {
                Nodes.Push(temp.Dequeue());
            }
            ComputeRootHash();
        }

        private IHash<T> FindCache(IHash<T> hash1, IHash<T> hash2)
        {
            var combineHash = hash1.Value.ToHex() + hash2.Value.ToHex();
            return _cache.TryGetValue(combineHash, out var resultHash)
                ? resultHash
                : AddCache(combineHash, new Hash<T>(hash1.CalculateHashWith(hash2)));
        }

        private IHash<T> AddCache(string keyHash, IHash<T> valueHash)
        {
            _cache[keyHash] = valueHash;
            return valueHash;
        }
    }
}
