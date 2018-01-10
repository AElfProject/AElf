using System.Collections.Generic;
using System.Security.Cryptography;

namespace AElf.Kernel
{
    public static class ExtensionMethods
    {
        public static byte[] ComputeHash(this byte[] buffer)
        {
            return SHA256.Create().ComputeHash(buffer);
        }

        /// <summary>
        /// Recursively compute first two hashes as well as replace them by one hash,
        /// until there is only one hash in the list.
        /// </summary>
        /// <param name="hashlist"></param>
        /// <returns>Finally return the list have only one element.</returns>
        public static List<Hash> ComputeProofHash(this List<Hash> hashlist)
        {
            if (hashlist.Count < 2)
                return hashlist;

            List<Hash> list = new List<Hash>
            {
                new Hash(hashlist[0], hashlist[1])
            };

            if (hashlist.Count > 2)
                hashlist.GetRange(2, hashlist.Count - 2).ForEach(h => list.Add(h));

            return ComputeProofHash(list);
        }


        public static List<ProofNode> GetProofList(this MerkleTree tree, Hash hash)
        {
            List<ProofNode> prooflist = new List<ProofNode>();
            MerkleNode node = tree.FindLeaf(hash);
            MerkleNode parent = node.ParentNode;
            while (parent != null)
            {
                ProofNode next = new ProofNode()
                {
                    Hash = parent.LeftNode == node ? parent.RightNode.Hash : parent.LeftNode.Hash,
                    Side = parent.LeftNode == node ? ProofNode.NodeSide.Right : ProofNode.NodeSide.Left
                };
                prooflist.Add(next);
                node = parent;
                parent = parent.ParentNode;
            }
            prooflist.Add(new ProofNode()
            {
                Hash = node.Hash,
                Side = ProofNode.NodeSide.Root
            });
            return prooflist;
        }
    }
}
