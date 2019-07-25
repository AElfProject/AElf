using System.Collections.Generic;
using System.Linq;
using System.Text;
using AElf.Types;
using Google.Protobuf;
using Virgil.Crypto;
using Xunit;

namespace AElf.Kernel.Types.Tests
{
    /// <summary>
    /// Use Transaction to be the type of the merkle node.
    /// </summary>
    public class MerkleTest
    {
        private Hash GetHashFromStrings(params string[] strings)
        {
            return strings.Select(Hash.FromString).ToList().ComputeBinaryMerkleTreeRootWithLeafNodes();
        }

        private Hash GetHashFromHexString(params string[] strings)
        {
            var hash = Hash.FromByteArray(ByteArrayHelper.FromHexString(strings[0]));
            foreach (var s in strings.Skip(1))
            {
                hash = Hash.FromRawBytes(hash.ToByteArray().Concat(ByteArrayHelper.FromHexString(s)).ToArray());
            }

            return hash;
        }
            
        /// <summary>
        /// Add node(s) and compute root hash
        /// </summary>
        [Fact]
        public void SingleNodeTest()
        {
            var tree = new BinaryMerkleTree();

            string hex = "5a7d71da020cae179a0dfe82bd3c967e1573377578f4cc87bc21f74f2556c0ef";
            tree.AddNode(CreateLeafFromHex(hex));

            //See if the hash of merkle tree is equal to the element’s hash.
            var root = tree.ComputeRootHash();
            var expected = GetHashFromHexString(hex, hex);
            Assert.Equal(expected, root);
        }

        [Fact]
        public void MerkleProofTest()
        {
            var tree = new BinaryMerkleTree();

            string hex = "5a7d71da020cae179a0dfe82bd3c967e1573377578f4cc87bc21f74f2556c0ef";
            var leaf = CreateLeafFromHex(hex);
            tree.AddNode(leaf);

            //See if the hash of merkle tree is equal to the element’s hash.
            var root = tree.ComputeRootHash();
            var path = tree.GenerateMerklePath(0);
            var calculatedRoot = path.ComputeBinaryMerkleTreeRootWithPathAndLeafNode(leaf);
            Assert.Equal(root, calculatedRoot);
        }
        
        [Fact]
        public void MerkleProofTest_MultiTwoLeaves()
        {
            var tree = new BinaryMerkleTree();

            string hex1 = "5a7d71da020cae179a0dfe82bd3c967e1573377578f4cc87bc21f74f2556c0ef";
            var leaf1 = CreateLeafFromHex(hex1);
            
            string hex2 = "a28bf94d0491a234d1e99abc62ed344eb55bb11aeecacc35c1b75bfa85c8983f";
            var leaf2 = CreateLeafFromHex(hex2);
            
            tree.AddNodes(new[] {leaf1, leaf2});

            //See if the hash of merkle tree is equal to the element’s hash.
            var root = tree.ComputeRootHash();
            var path = tree.GenerateMerklePath(0);
            var calculatedRoot = path.ComputeBinaryMerkleTreeRootWithPathAndLeafNode(leaf1);
            Assert.Equal(root, calculatedRoot);
        }
        
        [Fact]
        public void MerkleProofTest_MultiThreeLeaves()
        {
            var tree = new BinaryMerkleTree();

            string hex1 = "5a7d71da020cae179a0dfe82bd3c967e1573377578f4cc87bc21f74f2556c0ef";
            var leaf1 = CreateLeafFromHex(hex1);
            
            string hex2 = "a28bf94d0491a234d1e99abc62ed344eb55bb11aeecacc35c1b75bfa85c8983f";
            var leaf2 = CreateLeafFromHex(hex2);
            
            string hex3 = "bf6ae8809d017f07b27ad1620839c6503666fb55f7fe7ac70881e8864ce5a3ff";
            var leaf3 = CreateLeafFromHex(hex3);
            tree.AddNodes(new[] {leaf1, leaf2, leaf3});

            var root = tree.ComputeRootHash();
            var path = tree.GenerateMerklePath(2);
            var calculatedRoot = path.ComputeBinaryMerkleTreeRootWithPathAndLeafNode(leaf3);
            Assert.Equal(root, calculatedRoot);
        }
            
        [Fact]
        public void MerkleProofTest_MultiLeaves()
        {
            var tree = new BinaryMerkleTree();

            string hex1 = "5a7d71da020cae179a0dfe82bd3c967e1573377578f4cc87bc21f74f2556c0ef";
            var hash1 = CreateLeafFromHex(hex1);
            
            string hex2 = "a28bf94d0491a234d1e99abc62ed344eb55bb11aeecacc35c1b75bfa85c8983f";
            var hash2 = CreateLeafFromHex(hex2);
            
            string hex3 = "bf6ae8809d017f07b27ad1620839c6503666fb55f7fe7ac70881e8864ce5a3ff";
            var hash3 = CreateLeafFromHex(hex3);
            
            string hex4 = "bac4adcf8066921237320cdcddb721f5ba5d34065b9c54fe7f9893d8dfe52f17";
            var hash4 = CreateLeafFromHex(hex4);

            string hex5 = "bac4adcf8066921237320cdcddb721f5ba5d34065b9c54fe7f9893d8dfe52f17";
            var hash5 = CreateLeafFromHex(hex5);
            tree.AddNodes(new[] {hash1, hash2, hash3, hash4, hash5});

            //See if the hash of merkle tree is equal to the element’s hash.
            var root = tree.ComputeRootHash();
            var path = tree.GenerateMerklePath(4);
            var calculatedRoot = path.ComputeBinaryMerkleTreeRootWithPathAndLeafNode(hash5);
            //Assert.Contains(hash3, path.Path);
            Assert.Equal(root, calculatedRoot);
        }

        [Fact]
        public void MultiNodesTest()
        {
            var tree1 = new BinaryMerkleTree();
            tree1.AddNodes(CreateLeaves(new[] {"a", "e"}));

            //See if the hash of merkle tree is equal to the element’s hash.
            var root1 = tree1.ComputeRootHash();
            Assert.Equal(GetHashFromStrings("a", "e"), root1);

            var tree2 = new BinaryMerkleTree();
            tree2.AddNodes(CreateLeaves(new[] {"a", "e", "l"}));
            var root2 = tree2.ComputeRootHash();
            Hash right = GetHashFromStrings("l", "l");
            Assert.Equal(root1.ComputeParentNodeWith(right), root2);

            var tree3 = new BinaryMerkleTree();
            tree3.AddNodes(CreateLeaves(new[] {"a", "e", "l", "f"}));
            var root3 = tree3.ComputeRootHash();
            Hash right2 = GetHashFromStrings("l", "f");
            Assert.Equal(root1.ComputeParentNodeWith(right2), root3);

            var tree4 = new BinaryMerkleTree();
            tree4.AddNodes(CreateLeaves(new[] {"a", "e", "l", "f", "a"}));
            var root4 = tree4.ComputeRootHash();
            Hash l2 = GetHashFromStrings("a", "a");
            Hash l3 = l2.ComputeParentNodeWith(l2);
            Assert.Equal(root3.ComputeParentNodeWith(l3), root4);
        }

        [Fact]
        public void MerklePathTest1LeafNode()
        {
            var hashes = CreateLeaves(new[] {"a"});
            var tree = new BinaryMerkleTree();
            tree.AddNodes(hashes);
            var path = tree.GenerateMerklePath(0);
            Assert.Null(path);
            tree.ComputeRootHash();
            // test invalid index
            path = tree.GenerateMerklePath(1);
            Assert.Null(path);

            // test 1st "a"
            path = tree.GenerateMerklePath(0);
            Assert.NotNull(path);
            Assert.True(1 == path.MerklePathNodes.Count);
            var realPath = GenerateMerklePath(new[] {1}, tree.Nodes);
            Assert.Equal(realPath, path);
        }

        [Fact]
        public void MerklePathTest2LeafNodes()
        {
            var hashes = CreateLeaves(new[] {"a", "e"});
            var tree = new BinaryMerkleTree();
            tree.AddNodes(hashes);
            var path = tree.GenerateMerklePath(0);
            Assert.Null(path);
            tree.ComputeRootHash();
            // test invalid index
            path = tree.GenerateMerklePath(2);
            Assert.Null(path);

            // test "a"
            path = tree.GenerateMerklePath(0);
            Assert.NotNull(path);
            Assert.True(1 == path.MerklePathNodes.Count);
            var realPath1 = GenerateMerklePath(new[] {1}, tree.Nodes);
            Assert.Equal(realPath1, path);

            // test "e"
            path = tree.GenerateMerklePath(1);
            Assert.NotNull(path);
            Assert.True(1 == path.MerklePathNodes.Count);
            var realPath2 = GenerateMerklePath(new[] {0}, tree.Nodes);
            Assert.Equal(realPath2, path);
        }

        [Fact]
        public void MerklePathTest5LeafNodes()
        {
            var hashes = CreateLeaves(new[] {"a", "e", "l", "f", "a"});
            var tree = new BinaryMerkleTree();
            tree.AddNodes(hashes);
            var path = tree.GenerateMerklePath(5);
            Assert.Null(path);
            var root = tree.ComputeRootHash();

            // test 1st "a"
            var path1 = tree.GenerateMerklePath(0);
            Assert.NotNull(path1);
            Assert.Equal(3, path1.MerklePathNodes.Count);
            var realPath1 = GenerateMerklePath(new[] {1, 7, 11}, tree.Nodes);
            Assert.Equal(realPath1, path1);
            var actualRoot1 = ComputeRootWithMerklePathAndLeaf(Hash.FromString("a"), path1);
            Assert.Equal(root, actualRoot1);

            // test 1st "e"
            var path2 = tree.GenerateMerklePath(1);
            Assert.NotNull(path2);
            Assert.Equal(3, path2.MerklePathNodes.Count);
            var realPath2 = GenerateMerklePath(new[] {0, 7, 11}, tree.Nodes);

            Assert.Equal(realPath2, path2);
            var actualRoot2 = ComputeRootWithMerklePathAndLeaf(Hash.FromString("e"), path2);
            Assert.Equal(root, actualRoot2);

            // test 1st "l"
            var path3 = tree.GenerateMerklePath(2);
            Assert.NotNull(path3);
            Assert.Equal(3, path3.MerklePathNodes.Count);
            var realPath3 = GenerateMerklePath(new[] {3, 6, 11}, tree.Nodes);
            Assert.Equal(realPath3, path3);
            var actualRoot3 = ComputeRootWithMerklePathAndLeaf(Hash.FromString("l"), path3);
            Assert.Equal(root, actualRoot3);

            // test "f"
            var path4 = tree.GenerateMerklePath(3);
            Assert.NotNull(path4);
            Assert.Equal(3, path4.MerklePathNodes.Count);
            var realPath4 = GenerateMerklePath(new[] {2, 6, 11}, tree.Nodes);
            Assert.Equal(realPath4, path4);
            var actualRoot4 = ComputeRootWithMerklePathAndLeaf(Hash.FromString("f"), path4);
            Assert.Equal(root, actualRoot4);

            // test 2nd "a"
            var path5 = tree.GenerateMerklePath(4);
            Assert.NotNull(path5);
            Assert.Equal(3, path5.MerklePathNodes.Count);
            var realPath5 = GenerateMerklePath(new[] {5, 9, 10}, tree.Nodes);
            Assert.Equal(realPath5, path5);
            var actualRoot5 = ComputeRootWithMerklePathAndLeaf(Hash.FromString("a"), path5);
            Assert.Equal(root, actualRoot5);

            // test invalid index
            var path6 = tree.GenerateMerklePath(5);
            Assert.Null(path6);
        }

        [Theory]
        [InlineData(16, 0)]
        [InlineData(16, 15)]
        [InlineData(9, 8)]
        public void MerklePathTest(int leaveCount, int index)
        {
            var hashes = CreateLeaves(leaveCount);
            var bmt = new BinaryMerkleTree();
            bmt.AddNodes(hashes);
            var root = bmt.ComputeRootHash();
            var path = bmt.GenerateMerklePath(index);
            var calculatedRoot = path.ComputeBinaryMerkleTreeRootWithPathAndLeafNode(hashes[index]);
            Assert.Equal(root, calculatedRoot);
        }

        #region Some useful methods
        
        private List<Hash> CreateLeaves(IEnumerable<string> buffers)
        {
            return buffers.Select(Hash.FromString).ToList();
        }

        private List<Hash> CreateLeaves(int i)
        {
            List<Hash> res = new List<Hash>();
            for (int j = 0; j < i; j++)
            {
                res.Add(Hash.FromString(j.ToString()));
            }

            return res;
        }

        private Hash CreateLeafFromHex(string hex)
        {
            return Hash.FromByteArray(ByteArrayHelper.FromHexString(hex));
        }

        private Hash ComputeRootWithMerklePathAndLeaf(Hash leaf, MerklePath path)
        {
            return path.ComputeBinaryMerkleTreeRootWithPathAndLeafNode(leaf);
        }

        private MerklePath GenerateMerklePath(IList<int> index, IList<Hash> hashes)
        {
            var merklePath = new MerklePath();
            foreach (var i in index)
            {
                merklePath.MerklePathNodes.Add(new MerklePathNode
                {
                    Hash = hashes[i],
                    IsLeftChildNode = i % 2 == 0
                });
            }

            return merklePath;
        }
        
        #endregion
    }
}