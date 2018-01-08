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
            MerkleNode left = new MerkleNode
            {
                Hash = new MerkleHash("aelf")
            };

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

        [Fact]
        public void GenerateTreeWithOddLeaves()
        {
            MerkleTree tree = new MerkleTree();
            tree.AddLeaves(CreateLeaves(new string[] { "e", "l", "f" }));
            tree.GenerateMerkleTree();
            Assert.NotNull(tree.MerkleRoot);
        }

        [Fact]
        public void ProofListTest()
        {
            MerkleTree tree = new MerkleTree();
            tree.AddLeaves(CreateLeaves(new string[] { "a", "e", "l", "f", "2", "0", "1", "8" }));
            tree.GenerateMerkleTree();

            MerkleHash target = new MerkleHash("e");
            var prooflist = tree.GetProofList(target);

            Assert.True(prooflist[0].Hash.ToString() == new MerkleHash("a").ToString());
            Assert.True(prooflist[prooflist.Count - 1].Hash.ToString() == tree.MerkleRoot.Hash.ToString());
        }

        #region Some methods

        private static MerkleNode CreateNode(string buffer1, string buffer2)
        {
            MerkleNode left = new MerkleNode
            {
                Hash = new MerkleHash(buffer1)
            };
            MerkleNode right = new MerkleNode
            {
                Hash = new MerkleHash(buffer2)
            };

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
                MerkleNode node = new MerkleNode
                {
                    Hash = hash
                };
                leaves.Add(node);
            }
            return leaves;
        }

        #endregion
    }
}
