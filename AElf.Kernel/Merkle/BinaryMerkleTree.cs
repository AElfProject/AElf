using AElf.Kernel.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel
{
    public class BinaryMerkleTree<T> : IMerkleTree<T>
    {
        /// <summary>
        /// Merkle nodes
        /// </summary>
        public List<IHash<T>> Nodes { get; protected set; } = new List<IHash<T>>();

        /// <summary>
        /// Add a leaf node and compute root hash.
        /// </summary>
        /// <param name="hash"></param>
        public void AddNode(IHash<T> hash)
        {
            Nodes.Add(hash);
            ComputeRootHash();
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

        public IHash<T> FindLeaf(IHash<T> leaf) => Nodes.FirstOrDefault(l => l == leaf);

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
    }
}