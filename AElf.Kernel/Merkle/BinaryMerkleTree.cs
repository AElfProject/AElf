using AElf.Kernel.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel.Merkle
{
    public class BinaryMerkleTree<T> : IMerkleTree<T>
    {
        /// <summary>
        /// Merkle nodes
        /// </summary>
        private List<IHash<T>> Nodes { get; set; } = new List<IHash<T>>();

        /// <summary>
        /// Add a leaf node and compute root hash.
        /// </summary>
        /// <param name="hash"></param>
        public void AddNode(IHash<T> hash)
        {
            Nodes.Add(hash);
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
            hashes.ForEach(hash => Nodes.Add(hash));

            return this;
        }

        public IHash<IMerkleTree<T>> ComputeRootHash() => ComputeRootHash(Nodes);

        public IHash<IMerkleTree<T>> ComputeRootHash(List<IHash<T>> hashes)
        {
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
                List<IHash<T>> parents = new List<IHash<T>>();

                for (int i = 0; i < hashes.Count; i += 2)
                {
                    IHash<T> right = (i + 1 < hashes.Count) ? new Hash<T>(hashes[i + 1].Value) : null;
                    IHash<T> parent = new Hash<T>((hashes[i].ToString() + right?.ToString()).CalculateHash());

                    parents.Add(parent);
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
                if (Nodes[i] == leaf)
                {
                    return i;
                }
            }
            return -1;
        }

        public bool VerifyProofList(List<Hash<ITransaction>> hashlist)
        {
            List<Hash<ITransaction>> list = ComputeProofHash(hashlist);
            return ComputeRootHash().ToString() == list[0].ToString();
        }

        private List<Hash<ITransaction>> ComputeProofHash(List<Hash<ITransaction>> hashlist)
        {
            if (hashlist.Count < 2)
                return hashlist;

            List<Hash<ITransaction>> list = new List<Hash<ITransaction>>()
            {
                new Hash<ITransaction>((hashlist[0].ToString() + hashlist[1].ToString()).CalculateHash())
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
            Nodes[oldLeafOrder] = newLeaf;
            // TODO:
            // Make it quicker to compute root hash value.
            ComputeRootHash();
        }
    }
}
