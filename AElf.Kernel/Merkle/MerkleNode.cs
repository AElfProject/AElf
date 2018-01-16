using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AElf.Kernel
{
    public class MerkleNode : IMerkleNode
    {
        public MerkleNode LeftNode { get; set; }
        /// <summary>
        /// Regard the right node is null if it doesn't exist.
        /// </summaryMerkleNode
        public MerkleNode RightNode { get; set; }
        public MerkleNode ParentNode { get; set; }
        public Hash<IMerkleNode> Hash { get; set; }

        public MerkleNode() { }

        public MerkleNode(MerkleNode left, MerkleNode right = null)
        {
            LeftNode = left;
            RightNode = right;
            LeftNode.ParentNode = this;
            if (RightNode != null)
            {
                RightNode.ParentNode = this;
            }

            ComputeHash();
        }

        /// <summary>
        /// Set left node then directly compute hash.
        /// </summary>
        public void SetLeftNode(MerkleNode left)
        {
            if (left.Hash == null)
            {
                throw new AELFException("Merkle node did not initialized.");
            }
            LeftNode = left;
            LeftNode.ParentNode = this;

            ComputeHash();
        }

        /// <summary>
        /// Set right node then compute hash if left node is not null.
        /// </summary>
        public void SetRightNode(MerkleNode right)
        {
            if (right.Hash == null)
            {
                throw new AELFException("Merkle node did not initialized.");
            }
            RightNode = right;
            RightNode.ParentNode = this;

            if (LeftNode != null)
            {
                ComputeHash();
            }
        }

        /// <summary>
        /// Compute hash value as well as update the merkle tree.
        /// </summary>
        public void ComputeHash()
        {
            if (RightNode == null)
            {
                Hash = LeftNode.Hash;
            }
            else
            {
                Hash = new Hash<IMerkleNode>(
                    SHA256.Create().ComputeHash(
                        Encoding.UTF8.GetBytes(LeftNode.Hash.ToString() + RightNode.Hash.ToString())));
            }

            ParentNode?.ComputeHash();//Recursely update the hash value of parent node
        }

        /// <summary>
        /// Verify the hash value itself.
        /// </summary>
        /// <returns></returns>
        public bool VerifyHash()
        {
            //Nothing to verify
            if (LeftNode == null && RightNode == null) return true;

            //If right node is null, verify the left node.
            if (RightNode == null)
                return Hash.Value.SequenceEqual(LeftNode.Hash.Value);

            return Hash.Value.SequenceEqual(
                new Hash<IMerkleNode>(
                    SHA256.Create().ComputeHash(
                        Encoding.UTF8.GetBytes(LeftNode.Hash.ToString() + RightNode.Hash.ToString()))).Value);
        }

        #region Implementation of IEnumerable<MerkleNode>

        public IEnumerator<MerkleNode> GetEnumerator() => GetEnumerator();

        IEnumerator<IMerkleNode> IEnumerable<IMerkleNode>.GetEnumerator()
        {
            foreach (var n in Iterate(this))
                yield return n;
        }

        /// <summary>
        /// LRN / PostOrder
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected IEnumerable<IMerkleNode> Iterate(MerkleNode node)
        {
            if (node.LeftNode != null)
            {
                foreach (var n in Iterate(node.LeftNode))
                    yield return n;
            }

            if (node.RightNode != null)
            {
                foreach (var n in Iterate(node.RightNode))
                    yield return n;
            }

            yield return node;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var n in Iterate(this))
                yield return n;
        }

        #endregion
    }
}