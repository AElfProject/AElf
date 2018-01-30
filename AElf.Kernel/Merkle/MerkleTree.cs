using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AElf.Kernel
{
    [Serializable]
    public class MerkleTree<T> : IMerkleTree<T>
    {
        /// <summary>
        /// Merkle nodes
        /// </summary>
        public List<IHash<T>> Nodes { get; protected set; } = new List<IHash<T>>();

        /// <summary>
        /// Add a leaf at the same time.
        /// </summary>
        /// <param name="hash"></param>
        public void AddNode(IHash<T> hash)
        {
            Nodes.Add(hash);
            ComputeRootHash();
        }

        public MerkleTree<T> AddNodes(List<IHash<T>> hashes)
        {
            hashes.ForEach(hash => Nodes.Add(hash));

            return this;
        }

        public IHash<IMerkleTree<T>> ComputeRootHash()
        {
            return ComputeRootHash(Nodes);
        }

        private Hash<IMerkleTree<T>> CreateHash(byte[] hash)
        {
            return new Hash<IMerkleTree<T>>(hash);
        }

        public IHash<IMerkleTree<T>> ComputeRootHash(List<IHash<T>> hashes)
        {
            if (hashes.Count < 1)
            {
                throw new InvalidOperationException("Cannot generate merkle tree without any nodes.");
            }

            if (hashes.Count == 1)//Finally
            {
                return CreateHash(hashes[0].Value);
            }
            else
            {
                //Every time goes to a higher level.
                List<IHash<T>> parents = new List<IHash<T>>();

                for (int i = 0; i < hashes.Count; i += 2)
                {
                    IHash<T> right = (i + 1 < hashes.Count) ? new Hash<T>(hashes[i + 1].Value) : null;
                    IHash<T> parent = new Hash<T>((hashes[i].ToString() + right?.ToString()).GetSHA256Hash());

                    parents.Add(parent);
                }

                return ComputeRootHash(parents);
            }
        }

        public IHash<T> FindLeaf(IHash<T> leaf) => Nodes.FirstOrDefault(l => l == leaf);

        public bool VerifyProofList(List<Hash<ITransaction>> hashlist)
        {
            List<Hash<ITransaction>> list = hashlist.ComputeProofHash();
            return ComputeRootHash().ToString() == list[0].ToString();
        }
    }
}