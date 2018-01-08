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
