using Xunit;
using System.Collections.Generic;

namespace AElf.Kernel.Tests
{
    public class MerkleTest
    {
        [Fact]
        public void EqualHashes()
        {
            Hash h1 = new Hash("aelf");
            Hash h2 = new Hash("aelf");
            Assert.Equal(h1.Value, h2.Value);
        }

        [Fact]
        public void NotEqualHashes()
        {
            Hash h1 = new Hash("aelf");
            Hash h2 = new Hash("yifu");
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
                Hash = new Hash("aelf")
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
            tree.AddLeaves(CreateLeaves(new string[] { "a", "e", "l", "f" }))
                .Generate();

            Assert.NotNull(tree.MerkleRoot);
        }

        [Fact]
        public void GenerateTreeWithOddLeaves()
        {
            MerkleTree tree = new MerkleTree();
            tree.AddLeaves(CreateLeaves(new string[] { "e", "l", "f" }))
                .Generate();

            Assert.NotNull(tree.MerkleRoot);
        }

        [Fact]
        public void ProofListTest()
        {
            MerkleTree tree = new MerkleTree();
            tree.AddLeaves(CreateLeaves(new string[] { "a", "e", "l", "f", "2", "0", "1", "8" }))
                .Generate();

            Hash target = new Hash("e");
            var prooflist = tree.GetProofList(target);

            Assert.True(prooflist[0].Hash.ToString() == new Hash("a").ToString());
            Assert.True(prooflist[prooflist.Count - 1].Hash.ToString() == tree.MerkleRoot.Hash.ToString());
        }

        [Fact]
        public void VerifyProofListTest()
        {
            MerkleTree tree = new MerkleTree();
            tree.AddLeaves(CreateLeaves(new string[] { "a", "e", "l", "f" }))
                .Generate();

            #region Create elements of Proof List
            /* Merkle Tree:
             *                      root
             *  hash(hash(a),hash(e))   hash(hash(l),hash(f))
             *  hash(a)     hash(e)     hash(l)     hash(f)
             *      a        e          l           f
             */
            //Proof List: { hash(a), hash(e), hash(hash(l), hash(f)) }
            var hash_a = new Hash("a");

            var hash_e = new Hash("e");

            var hash_l = new Hash("l");
            var hash_f = new Hash("f");
            var hash_l_f = new Hash(hash_l, hash_f);
            #endregion

            List<Hash> prooflist = new List<Hash>
            {
                hash_a,
                hash_e,
                hash_l_f
            };

            Assert.True(tree.VerifyProofList(prooflist));
        }

        #region Some methods

        private static MerkleNode CreateNode(string buffer1, string buffer2)
        {
            MerkleNode left = new MerkleNode
            {
                Hash = new Hash(buffer1)
            };
            MerkleNode right = new MerkleNode
            {
                Hash = new Hash(buffer2)
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
                Hash hash = new Hash(buffer);
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
