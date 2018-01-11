using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace AElf.Kernel
{
    public static class ExtensionMethods
    {
        public static byte[] GetSHA256Hash(this ITransaction tx)
        {
            return SHA256.Create().ComputeHash(tx.GetHash().Value);
        }

        public static byte[] GetSHA256Hash(this string str)
        {
            return SHA256.Create().ComputeHash(
                Encoding.UTF8.GetBytes(str));
        }

        /// <summary>
        /// Recursively compute first two hashes as well as replace them by one hash,
        /// until there is only one hash in the list.
        /// </summary>
        /// <param name = "hashlist" ></ param >
        /// < returns > Finally return the list have only one element.</returns>
        public static List<Hash<ITransaction>> ComputeProofHash(this List<Hash<ITransaction>> hashlist)
        {
            if (hashlist.Count < 2)
                return hashlist;

            List<Hash<ITransaction>> list = new List<Hash<ITransaction>>()
            {
                new Hash<ITransaction>(
                    SHA256.Create().ComputeHash(
                        Encoding.UTF8.GetBytes(hashlist[0].ToString() + hashlist[1].ToString())))
            };

            if (hashlist.Count > 2)
                hashlist.GetRange(2, hashlist.Count - 2).ForEach(h => list.Add(h));

            return ComputeProofHash(list);
        }

        //TODO: Should build a whole merkle tree to get proof list.
        //public static List<ProofNode> GetProofList<T>(this MerkleTree<T> tree, Hash<T> hash)
        //{
        //    List<ProofNode> prooflist = new List<ProofNode>();
        //    MerkleNode node = tree.FindLeaf(hash);
        //    MerkleNode parent = node.ParentNode;
        //    while (parent != null)
        //    {
        //        ProofNode next = new ProofNode()
        //        {
        //            Hash = parent.LeftNode == node ? parent.RightNode.Hash : parent.LeftNode.Hash,
        //            Side = parent.LeftNode == node ? ProofNode.NodeSide.Right : ProofNode.NodeSide.Left
        //        };
        //        prooflist.Add(next);
        //        node = parent;
        //        parent = parent.ParentNode;
        //    }
        //    prooflist.Add(new ProofNode()
        //    {
        //        Hash = node.Hash,
        //        Side = ProofNode.NodeSide.Root
        //    });
        //    return prooflist;
        //}
    }
}