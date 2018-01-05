using Xunit;
using AElf.Kernel.Merkle;
using System.Collections.Generic;

namespace AElf.Kernel.Tests
{
    public class MerkleTest
    {
        [Fact]
        public void EqualHashes()
        {
            MerkleHash h1 = new MerkleHash("aelf");
            MerkleHash h2 = new MerkleHash("aelf");
            Assert.Equal(h1.Value, h2.Value);
        }

        [Fact]
        public void NotEqualHashes()
        {
            MerkleHash h1 = new MerkleHash("aelf");
            MerkleHash h2 = new MerkleHash("yifu");
            Assert.NotEqual(h1.Value, h2.Value);
        }

        [Fact]
        public void CreateEmptyNode()
        {
            MerkleNode node = new MerkleNode();
            Assert.Null(node.ParentNode);
            Assert.Null(node.LeftNode);
            Assert.Null(node.RightNode);
        }

        [Fact]
        public void VerifyLeftHash()
        {
            MerkleNode left = new MerkleNode();
            left.Hash = new MerkleHash("aelf");

            MerkleNode parent = new MerkleNode();
            parent.SetLeftNode(left);

            Assert.True(parent.VerifyHash());
        }

        [Fact]
        public void VerifyHash()
        {
            MerkleNode parent = CreateNode("ae", "lf");
            Assert.True(parent.VerifyHash());
        }

        [Fact]
        public void EqualNodes()
        {
            MerkleNode node1 = CreateNode("ae", "lf");
            MerkleNode node2 = CreateNode("ae", "lf");
            Assert.Equal(node1.Hash.Value, node2.Hash.Value);
        }

        [Fact]
        public void NotEqualNodes()
        {
            MerkleNode node1 = CreateNode("ae", "lf");
            MerkleNode node2 = CreateNode("yi", "fu");
            Assert.NotEqual(node1.Hash.Value, node2.Hash.Value);
        }

        [Fact]
        public void GenerateTreeWithEvenLeaves()
        {
            MerkleTree tree = new MerkleTree();
            tree.AddLeaves(CreateLeaves(new string[] { "a", "e", "l", "f" }));
            tree.GenerateMerkleTree();
            Assert.NotNull(tree.MerkleRoot);
        }

        #region Some methods

        private static MerkleNode CreateNode(string buffer1, string buffer2)
        {
            MerkleNode left = new MerkleNode();
            MerkleNode right = new MerkleNode();
            left.Hash = new MerkleHash(buffer1);
            right.Hash = new MerkleHash(buffer2);

            MerkleNode parent = new MerkleNode();
            parent.SetLeftNode(left);
            parent.SetRightNode(right);

            return parent;
        }

        private static List<MerkleNode> CreateLeaves(string[] buffers)
        {
            List<MerkleNode> leaves = new List<MerkleNode>();
            foreach (var buffer in buffers)
            {
                MerkleHash hash = new MerkleHash(buffer);
                MerkleNode node = new MerkleNode();
                node.Hash = hash;
                leaves.Add(node);
            }
            return leaves;
        }

        #endregion
    }
}
