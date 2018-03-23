using AElf.Kernel.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AElf.Kernel.Merkle
{
    public class BinaryMerkleNode : IMerkleNode
    {
        public BinaryMerkleNode LeftNode { get; set; }

        /// <summary>
        /// Regard the right node is null if it doesn't exist.
        /// </summary>
        public BinaryMerkleNode RightNode { get; set; }
        public BinaryMerkleNode ParentNode { get; set; }
        public Hash<IMerkleNode> Hash { get; set; }

        public BinaryMerkleNode(BinaryMerkleNode left, BinaryMerkleNode right = null)
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
        public void SetLeftNode(BinaryMerkleNode left)
        {
            if (left.Hash == null)
            {
                throw new InvalidOperationException("Merkle node did not initialized.");
            }
            LeftNode = left;
            LeftNode.ParentNode = this;

            ComputeHash();
        }

        /// <summary>
        /// Set right node then compute hash if left node is not null.
        /// </summary>
        public void SetRightNode(BinaryMerkleNode right)
        {
            if (right.Hash == null)
            {
                throw new InvalidOperationException("Merkle node did not initialized.");
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
            Hash = RightNode == null ? 
                LeftNode.Hash : 
                new Hash<IMerkleNode>(LeftNode.Hash.CalculateHashWith(RightNode.Hash));

            ParentNode?.ComputeHash();//Recursely update the hash value of parent node
        }

        /// <summary>
        /// Verify the hash value itself.
        /// </summary>
        /// <returns></returns>
        public bool VerifyHash()
        {
            //Nothing to verify
            if (LeftNode == null) return true;

            //If right node is null, verify the left node.
            if (RightNode == null)
                return LeftNode != null && Hash.Value.SequenceEqual(LeftNode.Hash.Value);

            return Hash.Equals(
                new Hash<IMerkleNode>(
                    LeftNode.Hash.CalculateHashWith(RightNode.Hash)));
        }

        #region Implementation of IEnumerable<MerkleNode>

        IEnumerator<IMerkleNode> IEnumerable<IMerkleNode>.GetEnumerator()
        {
            return Iterate(this).GetEnumerator();
        }

        /// <summary>
        /// LRN / PostOrder
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private IEnumerable<IMerkleNode> Iterate(BinaryMerkleNode node)
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
            return Iterate(this).GetEnumerator();
        }

        #endregion
        
        public byte[] Serialize()
        {
            throw new NotImplementedException();
        }
    }
}