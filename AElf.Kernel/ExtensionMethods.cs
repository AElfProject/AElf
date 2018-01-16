using Newtonsoft.Json;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace AElf.Kernel
{
    public static class ExtensionMethods
    {
        public static byte[] GetHash(this object obj)
        {
            return SHA256.Create().ComputeHash(
                Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(obj)));
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
                new Hash<ITransaction>((hashlist[0].ToString() + hashlist[1].ToString()).GetHash())
            };

            if (hashlist.Count > 2)
                hashlist.GetRange(2, hashlist.Count - 2).ForEach(h => list.Add(h));

            return ComputeProofHash(list);
        }

        public static int NumberOfZero(this byte[] hash)
        {
            int number = 0;
            while (hash[number] == 0)
            {
                number++;
            }
            return number;
        }

        public static bool TryDequeue<T>(this Queue<T> queue, out T t)
        {
            if (queue.Count > 0)
            {
                t = queue.Dequeue();
                return true;
            }
            else
            {
                t = default(T);
                return false;
            }
        }

        #region TODO: Should build a whole merkle tree to get proof list.
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
        #endregion
    }
}