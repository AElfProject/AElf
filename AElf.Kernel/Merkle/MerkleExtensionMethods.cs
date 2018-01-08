using System.Collections.Generic;
using System.Security.Cryptography;

namespace AElf.Kernel.Merkle
{
    public static class MerkleExtensionMethods
    {
        public static byte[] ComputeHash(this byte[] buffer)
        {
            return SHA256.Create().ComputeHash(buffer);
        }

        public static List<MerkleHash> ComputeProofHash(this List<MerkleHash> hashlist)
        {
            if (hashlist.Count < 2)
                return hashlist;

            List<MerkleHash> list = new List<MerkleHash>();
            list.Add(new MerkleHash(hashlist[0], hashlist[1]));

            if (hashlist.Count > 2)
                hashlist.GetRange(2, hashlist.Count - 2).ForEach(h => list.Add(h));

            return ComputeProofHash(list);
        }


        public static List<ProofNode> GetProofList(this MerkleTree tree, MerkleHash hash)
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
