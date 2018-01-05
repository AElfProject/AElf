using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AElf.Kernel.MerkleTree
{
    public class MerkleNode : IEnumerable<MerkleNode>
    {
        public MerkleNode LeftNode { get; set; }
        /// <summary>
        /// Regard the right node is null if it doesn't exist.
        /// </summary>
        public MerkleNode RightNode { get; set; }
        public MerkleNode ParentNode { get; set; }
        public MerkleHash Hash { get; set; }

        /// <summary>
        /// Set left node then directly compute hash.
        /// </summary>
        public void SetLeftNode(MerkleNode left)
        {
            if (left.Hash == null)
            {
                throw new MerkleException("Merkle node did not initialized.");
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
                throw new MerkleException("Merkle node did not initialized.");
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
        private void ComputeHash()
        {
            if (RightNode == null)
            {
                Hash = LeftNode.Hash;
            }
            else
            {
                Hash = new MerkleHash(LeftNode.Hash, RightNode.Hash);
            }

            ParentNode?.ComputeHash();//Recursely update the hash value of parent node
        }

        #region Implement of IEnumerable<MerkleNode>

        public IEnumerator<MerkleNode> GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var n in Iterate(this))
                yield return n;
        }

        /// <summary>
        /// LRN / PostOrder
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected IEnumerable<MerkleNode> Iterate(MerkleNode node)
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

        #endregion
    }
}
