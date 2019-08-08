using System.Collections.Generic;
using System.Linq;
using System.Text;
using AElf.Types;
using Google.Protobuf;
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
            var hash = Hash.FromByteArray(ByteArrayHelper.HexStringToByteArray(strings[0]));
            foreach (var s in strings.Skip(1))
            {
                hash = Hash.FromRawBytes(hash.ToByteArray().Concat(ByteArrayHelper.HexStringToByteArray(s)).ToArray());
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
            tree.AddNode(CreateLeafFromHex(hex));

            //See if the hash of merkle tree is equal to the element’s hash.
            var root = tree.ComputeRootHash();
            var path = tree.GenerateMerklePath(0);
            var hash = Hash.FromByteArray(ByteArrayHelper.HexStringToByteArray(hex));
            var calculatedRoot = path.ComputeBinaryMerkleTreeRootWithLeafNodes();
            Assert.Contains(hash, path);
            Assert.Equal(root, calculatedRoot);
        }
        
        [Fact]
        public void MerkleProofTest_MultiTwoSameLeaves()
        {
            var tree = new BinaryMerkleTree();

            string hex = "5a7d71da020cae179a0dfe82bd3c967e1573377578f4cc87bc21f74f2556c0ef";
            var hash = CreateLeafFromHex(hex);
            
            tree.AddNodes(new []{hash, hash});

            //See if the hash of merkle tree is equal to the element’s hash.
            var root = tree.ComputeRootHash();
            var path = tree.GenerateMerklePath(1);
            var calculatedRoot = path.ComputeBinaryMerkleTreeRootWithLeafNodes();
            Assert.Contains(hash, path);
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
            var hash5 = Hash.FromRawBytes(ByteArrayHelper.HexStringToByteArray(hex5)
                .Concat(Encoding.UTF8.GetBytes(TransactionResultStatus.Mined.ToString())).ToArray());
            tree.AddNodes(new []{hash1, hash2, hash3, hash4, hash5});

            //See if the hash of merkle tree is equal to the element’s hash.
            var root = tree.ComputeRootHash();
            var path = tree.GenerateMerklePath(4);
            var calculatedRoot = path.ComputeBinaryMerkleTreeRootWithPathAndLeafNode(hash5);
            //Assert.Contains(hash3, path.Path);
            Assert.Equal(root, calculatedRoot);
        }

        [Fact]
        public void Test()
        {
            string base64 ="CiBbTv9+r6QF+6wdIX6uzCHiZBIjYtU7mhP0ybyLGYgUKQ==";
            var hash = Hash.Parser.ParseFrom(ByteString.FromBase64(base64));
            ;
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
            Assert.True(1 == path.Count);
            var realPath = new List<Hash>{tree.Nodes[1]};
            Assert.Equal(realPath, path.ToList());
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
            Assert.True(1 == path.Count);
            var realPath = new List<Hash>{tree.Nodes[1]};
            Assert.Equal(realPath, path.ToList());

            // test "e"
            path = tree.GenerateMerklePath(1);
            Assert.NotNull(path);
            Assert.True(1 == path.Count);
            realPath = new List<Hash>{tree.Nodes[0]};
            Assert.Equal(realPath, path.ToList());
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
            path = tree.GenerateMerklePath(0);
            Assert.NotNull(path);
            Assert.Equal(3, path.Count);
            var realPath = new List<Hash>{tree.Nodes[1], tree.Nodes[7], tree.Nodes[11]};
            Assert.Equal(realPath, path.ToList());
            var calroot = ComputeMerklePath(Hash.FromString("a"), path);
            Assert.Equal(root, calroot);

            // test 1st "e"
            path = tree.GenerateMerklePath(1);
            Assert.NotNull(path);
            Assert.Equal(3, path.Count);
            realPath = new List<Hash>{tree.Nodes[0], tree.Nodes[7], tree.Nodes[11]};
            Assert.Equal(realPath, path.ToList());
            calroot = ComputeMerklePath(Hash.FromString("e"), path);
            Assert.Equal(root, calroot);

            // test 1st "l"
            path = tree.GenerateMerklePath(2);
            Assert.NotNull(path);
            Assert.Equal(3, path.Count);
            realPath = new List<Hash>{tree.Nodes[3], tree.Nodes[6], tree.Nodes[11]};
            Assert.Equal(realPath, path.ToList());
            calroot = ComputeMerklePath(Hash.FromString("l"), path);
            Assert.Equal(root, calroot);

            // test "f"
            path = tree.GenerateMerklePath(3);
            Assert.NotNull(path);
            Assert.Equal(3, path.Count);
            realPath = new List<Hash>{tree.Nodes[2], tree.Nodes[6], tree.Nodes[11]};
            Assert.Equal(realPath, path.ToList());
            calroot = ComputeMerklePath(Hash.FromString("f"), path);
            Assert.Equal(root, calroot);

            // test 2nd "a"
            path = tree.GenerateMerklePath(4);
            Assert.NotNull(path);
            Assert.Equal(3, path.Count);
            realPath = new List<Hash>{tree.Nodes[5], tree.Nodes[9], tree.Nodes[10]};
            Assert.Equal(realPath, path.ToList());
            calroot = ComputeMerklePath(Hash.FromString("a"), path);
            Assert.Equal(root, calroot);

            // test invalid idnex
            path = tree.GenerateMerklePath(5);
            Assert.Null(path);
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

        private Hash CreateLeaf(string buffer)
        {
            return Hash.FromString(buffer);
        }

        private Hash CreateLeafFromHex(string hex)
        {
            return Hash.FromByteArray(ByteArrayHelper.HexStringToByteArray(hex));
        }

        private Hash ComputeMerklePath(Hash leaf, IList<Hash> path)
        {
            return path.ComputeBinaryMerkleTreeRootWithPathAndLeafNode(leaf);
        }
        
        #endregion
    }
}