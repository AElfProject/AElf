using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AElf.Types.Tests
{
    public class BinaryMerkleTestTest
    {
        private Hash GetHashFromStrings(params string[] strings)
        {
            return BinaryMerkleTree.FromLeafNodes(strings.Select(HashHelper.ComputeFrom).ToList()).Root;
        }

        private Hash GetHashFromHexString(params string[] strings)
        {
            var hash = Hash.LoadFromByteArray(ByteArrayHelper.HexStringToByteArray(strings[0]));
            foreach (var s in strings.Skip(1))
            {
                hash = HashHelper.ComputeFrom(hash.ToByteArray().Concat(ByteArrayHelper.HexStringToByteArray(s)).ToArray());
            }

            return hash;
        }

        /// <summary>
        /// Add node(s) and compute root hash
        /// </summary>
        [Fact]
        public void SingleNodeTest()
        {
            string hex = "5a7d71da020cae179a0dfe82bd3c967e1573377578f4cc87bc21f74f2556c0ef";
            var tree = BinaryMerkleTree.FromLeafNodes(new[] {CreateLeafFromHex(hex)});

            //See if the hash of merkle tree is equal to the element’s hash.
            var root = tree.Root;
            var expected = GetHashFromHexString(hex, hex);
            Assert.Equal(expected, root);
        }

        [Fact]
        public void MerkleProofTest()
        {
            string hex = "5a7d71da020cae179a0dfe82bd3c967e1573377578f4cc87bc21f74f2556c0ef";
            var leaf = CreateLeafFromHex(hex);
            var tree = BinaryMerkleTree.FromLeafNodes(new[] {leaf});

            //See if the hash of merkle tree is equal to the element’s hash.
            var root = tree.Root;
            var path = tree.GenerateMerklePath(0);
            var calculatedRoot = path.ComputeRootWithLeafNode(leaf);
            Assert.Equal(root, calculatedRoot);
        }

        [Fact]
        public void MerkleProofTest_MultiTwoLeaves()
        {
            string hex1 = "5a7d71da020cae179a0dfe82bd3c967e1573377578f4cc87bc21f74f2556c0ef";
            var leaf1 = CreateLeafFromHex(hex1);

            string hex2 = "a28bf94d0491a234d1e99abc62ed344eb55bb11aeecacc35c1b75bfa85c8983f";
            var leaf2 = CreateLeafFromHex(hex2);

            var tree = BinaryMerkleTree.FromLeafNodes(new[] {leaf1, leaf2});

            //See if the hash of merkle tree is equal to the element’s hash.
            var root = tree.Root;
            var path = tree.GenerateMerklePath(0);
            var calculatedRoot = path.ComputeRootWithLeafNode(leaf1);
            Assert.Equal(root, calculatedRoot);
        }

        [Fact]
        public void MerkleProofTest_MultiThreeLeaves()
        {
            string hex1 = "5a7d71da020cae179a0dfe82bd3c967e1573377578f4cc87bc21f74f2556c0ef";
            var leaf1 = CreateLeafFromHex(hex1);

            string hex2 = "a28bf94d0491a234d1e99abc62ed344eb55bb11aeecacc35c1b75bfa85c8983f";
            var leaf2 = CreateLeafFromHex(hex2);

            string hex3 = "bf6ae8809d017f07b27ad1620839c6503666fb55f7fe7ac70881e8864ce5a3ff";
            var leaf3 = CreateLeafFromHex(hex3);
            var tree = BinaryMerkleTree.FromLeafNodes(new[] {leaf1, leaf2, leaf3});

            var root = tree.Root;
            var path = tree.GenerateMerklePath(2);
            var calculatedRoot = path.ComputeRootWithLeafNode(leaf3);
            Assert.Equal(root, calculatedRoot);
        }

        [Fact]
        public void MerkleProofTest_MultiLeaves()
        {
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
            var tree = BinaryMerkleTree.FromLeafNodes(new[] {hash1, hash2, hash3, hash4, hash5});

            //See if the hash of merkle tree is equal to the element’s hash.
            var root = tree.Root;
            var path = tree.GenerateMerklePath(4);
            var calculatedRoot = path.ComputeRootWithLeafNode(hash5);
            //Assert.Contains(hash3, path.Path);
            Assert.Equal(root, calculatedRoot);
        }

        [Fact]
        public void MultiNodesTest()
        {
            var tree1 = BinaryMerkleTree.FromLeafNodes(CreateLeaves(new[] {"a", "e"}));

            //See if the hash of merkle tree is equal to the element’s hash.
            var root1 = tree1.Root;
            Assert.Equal(GetHashFromStrings("a", "e"), root1);

            var tree2 = BinaryMerkleTree.FromLeafNodes(CreateLeaves(new[] {"a", "e", "l"}));
            var root2 = tree2.Root;
            Hash right = GetHashFromStrings("l", "l");
            Assert.Equal(HashHelper.ConcatAndCompute(root1, right), root2);

            var tree3 = BinaryMerkleTree.FromLeafNodes(CreateLeaves(new[] {"a", "e", "l", "f"}));
            var root3 = tree3.Root;
            Hash right2 = GetHashFromStrings("l", "f");
            Assert.Equal(HashHelper.ConcatAndCompute(root1, right2), root3);

            var tree4 = BinaryMerkleTree.FromLeafNodes(CreateLeaves(new[] {"a", "e", "l", "f", "a"}));
            var root4 = tree4.Root;
            Hash l2 = GetHashFromStrings("a", "a");
            Hash l3 = HashHelper.ConcatAndCompute(l2, l2);
            Assert.Equal(HashHelper.ConcatAndCompute(root3, l3), root4);
        }

        [Fact]
        public void MerklePathTest1LeafNode()
        {
            var hashes = CreateLeaves(new[] {"a"});
            var tree = BinaryMerkleTree.FromLeafNodes(hashes);

            // test invalid index
            Assert.Throws<InvalidOperationException>(() => tree.GenerateMerklePath(1));

            // test 1st "a"
            var path = tree.GenerateMerklePath(0);
            Assert.NotNull(path);
            Assert.True(1 == path.MerklePathNodes.Count);
            var realPath = GenerateMerklePath(new[] {1}, tree.Nodes);
            Assert.Equal(realPath, path);
        }

        [Fact]
        public void MerklePathTest2LeafNodes()
        {
            var hashes = CreateLeaves(new[] {"a", "e"});
            var tree = BinaryMerkleTree.FromLeafNodes(hashes);
            // test invalid index
            Assert.Throws<InvalidOperationException>(() => tree.GenerateMerklePath(2));

            // test "a"
            var path = tree.GenerateMerklePath(0);
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
            var tree = BinaryMerkleTree.FromLeafNodes(hashes);
            var root = tree.Root;

            // test 1st "a"
            var path1 = tree.GenerateMerklePath(0);
            Assert.NotNull(path1);
            Assert.Equal(3, path1.MerklePathNodes.Count);
            var realPath1 = GenerateMerklePath(new[] {1, 7, 11}, tree.Nodes);
            Assert.Equal(realPath1, path1);
            var actualRoot1 = ComputeRootWithMerklePathAndLeaf(HashHelper.ComputeFrom("a"), path1);
            Assert.Equal(root, actualRoot1);

            // test 1st "e"
            var path2 = tree.GenerateMerklePath(1);
            Assert.NotNull(path2);
            Assert.Equal(3, path2.MerklePathNodes.Count);
            var realPath2 = GenerateMerklePath(new[] {0, 7, 11}, tree.Nodes);

            Assert.Equal(realPath2, path2);
            var actualRoot2 = ComputeRootWithMerklePathAndLeaf(HashHelper.ComputeFrom("e"), path2);
            Assert.Equal(root, actualRoot2);

            // test 1st "l"
            var path3 = tree.GenerateMerklePath(2);
            Assert.NotNull(path3);
            Assert.Equal(3, path3.MerklePathNodes.Count);
            var realPath3 = GenerateMerklePath(new[] {3, 6, 11}, tree.Nodes);
            Assert.Equal(realPath3, path3);
            var actualRoot3 = ComputeRootWithMerklePathAndLeaf(HashHelper.ComputeFrom("l"), path3);
            Assert.Equal(root, actualRoot3);

            // test "f"
            var path4 = tree.GenerateMerklePath(3);
            Assert.NotNull(path4);
            Assert.Equal(3, path4.MerklePathNodes.Count);
            var realPath4 = GenerateMerklePath(new[] {2, 6, 11}, tree.Nodes);
            Assert.Equal(realPath4, path4);
            var actualRoot4 = ComputeRootWithMerklePathAndLeaf(HashHelper.ComputeFrom("f"), path4);
            Assert.Equal(root, actualRoot4);

            // test 2nd "a"
            var path5 = tree.GenerateMerklePath(4);
            Assert.NotNull(path5);
            Assert.Equal(3, path5.MerklePathNodes.Count);
            var realPath5 = GenerateMerklePath(new[] {5, 9, 10}, tree.Nodes);
            Assert.Equal(realPath5, path5);
            var actualRoot5 = ComputeRootWithMerklePathAndLeaf(HashHelper.ComputeFrom("a"), path5);
            Assert.Equal(root, actualRoot5);

            // test invalid index
            Assert.Throws<InvalidOperationException>(() => tree.GenerateMerklePath(5));
        }

        [Theory]
        [InlineData(16, 0)]
        [InlineData(16, 15)]
        [InlineData(9, 8)]
        public void MerklePathTest(int leafCount, int index)
        {
            var hashes = CreateLeaves(leafCount);
            var tree = BinaryMerkleTree.FromLeafNodes(hashes.ToArray());
            var root = tree.Root;
            var path = tree.GenerateMerklePath(index);
            var calculatedRoot = path.ComputeRootWithLeafNode(hashes[index]);
            Assert.Equal(root, calculatedRoot);
        }

        [Theory]
        [InlineData(4, 7)]
        [InlineData(5, 13)]
        [InlineData(6, 13)]
        [InlineData(7, 15)]
        [InlineData(9, 23)]
        public void MerkleNodesCountTest(int leafCount, int expectCount)
        {
            var hashes = CreateLeaves(leafCount);
            var tree = BinaryMerkleTree.FromLeafNodes(hashes);
            var nodesCount = tree.Nodes.Count;
            Assert.Equal(expectCount, nodesCount);
        }

        [Fact]
        public void TestMerklePathFromEth()
        {
            var expectedRoot =
                Hash.LoadFromByteArray(
                    ByteArrayHelper.HexStringToByteArray(
                        "0x90ae927b3312b71e5ea7c8644a9d6f4e6107bf6c0e06df094c94be16d8023c52"));
            {
                var hex =
                    "0x324144584c63794b4d47477252653961474337584d584543763863787a33546f73317a36504a4853667958677553615662350000000000000000000000000000000000000000000000001111d67bb1bb0000";
                var expectedHash =
                    Hash.LoadFromByteArray(
                        ByteArrayHelper.HexStringToByteArray(
                            "0xa80afe5c85c3e9b09e8c74070d5d8d4de60780968d78e7b031e939e7335b6916"));
                var actualHash = HashHelper.ComputeFrom(ByteArrayHelper.HexStringToByteArray(hex));
                Assert.Equal(expectedHash, actualHash);

                var merklePath = new MerklePath
                {
                    MerklePathNodes =
                    {
                        new MerklePathNode
                        {
                            IsLeftChildNode = true,
                            Hash = Hash.LoadFromByteArray(
                                ByteArrayHelper.HexStringToByteArray(
                                    "0x739c9a33d81d44b3c6b511c326337c12d9f557fdc3c9476ab33f5d064785a47e"))
                        },
                        new MerklePathNode
                        {
                            IsLeftChildNode = true,
                            Hash = Hash.LoadFromByteArray(
                                ByteArrayHelper.HexStringToByteArray(
                                    "0x8487ce95823a23aac06a040828761818b2d491fb6603c5d3284b3c9c73764c87"))
                        },
                        new MerklePathNode
                        {
                            IsLeftChildNode = true,
                            Hash = Hash.LoadFromByteArray(
                                ByteArrayHelper.HexStringToByteArray(
                                    "0x7c8e76b0b80181c5154d138078d15aafd3e980858b8eb9076a3cae3fcdec76be"))
                        }
                    }
                };

                var actualRoot = merklePath.ComputeRootWithLeafNode(actualHash);
                Assert.Equal(expectedRoot, actualRoot);
            }

            {
                var hex =
                    "0x536b4d476a766941417339626e59767636634b636166626866367462524751474b393357674b765a6f436f5335616d4d4b00000000000000000000000000000000000000000000000302379bf2ca2e0000";
                var expectedHash =
                    Hash.LoadFromByteArray(
                        ByteArrayHelper.HexStringToByteArray(
                            "0xdbd4b01cea12038a3b0c3ce4900c64635b96a1ee2331625fbe473d6c1e816548"));
                var actualHash = HashHelper.ComputeFrom(ByteArrayHelper.HexStringToByteArray(hex));
                Assert.Equal(expectedHash, actualHash);

                var merklePath = new MerklePath
                {
                    MerklePathNodes =
                    {
                        new MerklePathNode
                        {
                            IsLeftChildNode = false,
                            Hash = Hash.LoadFromByteArray(
                                ByteArrayHelper.HexStringToByteArray(
                                    "0x12e2fc90c9b2c6887bf9633bf4872db4bb8cfe18619b0900dad49286abb81248"))
                        },
                        new MerklePathNode
                        {
                            IsLeftChildNode = false,
                            Hash = Hash.LoadFromByteArray(
                                ByteArrayHelper.HexStringToByteArray(
                                    "0x56ac8fe5cfe48c5ddf5d40f9a8ed5e504929935b677376cae35b8c326a97d34b"))
                        },
                        new MerklePathNode
                        {
                            IsLeftChildNode = true,
                            Hash = Hash.LoadFromByteArray(
                                ByteArrayHelper.HexStringToByteArray(
                                    "0x7c8e76b0b80181c5154d138078d15aafd3e980858b8eb9076a3cae3fcdec76be"))
                        }
                    }
                };

                var actualRoot = merklePath.ComputeRootWithLeafNode(actualHash);
                Assert.Equal(expectedRoot, actualRoot);
            }
        }

        [Theory]
        [InlineData("SkMGjviAAs9bnYvv6cKcafbhf6tbRGQGK93WgKvZoCoS5amMK", "75900000000000000000",
            "96de8fc8c256fa1e1556d41af431cace7dca68707c78dd88c3acab8b17164c47",
            "0x0eb4305ab57ea86f1f2940cc32c86f5870c5463e0e9c57c6ead6a38cbb2ded90")]
        [InlineData("2ADXLcyKMGGrRe9aGC7XMXECv8cxz3Tos1z6PJHSfyXguSaVb5", "5500000000000000000",
            "d9147961436944f43cd99d28b2bbddbf452ef872b30c8279e255e7daafc7f946",
            "0xd4998a64d00f9b9178337fcebcb193494ceefc7b2ee68031a5060ef407d7ae2d")]
        public void CalculateHashTest(string address, string amount, string uid, string result)
        {
            var hashFromString = HashHelper.ComputeFrom(address);

            var parsedResult = decimal.Parse(amount);
            var originTokenSizeInByte = 32;
            var preHolderSize = originTokenSizeInByte - 16;
            var bytesFromDecimal = decimal.GetBits(parsedResult).Reverse().ToArray();

            if (preHolderSize < 0)
                bytesFromDecimal = bytesFromDecimal.TakeLast(originTokenSizeInByte).ToArray();

            var amountBytes = new List<byte>();
            bytesFromDecimal.Aggregate(amountBytes, (cur, i) =>
            {
                while (cur.Count < preHolderSize)
                {
                    cur.Add(new byte());
                }
                
                cur.AddRange(i.ToBytes());
                return cur;
            });
            var hashFromAmount = HashHelper.ComputeFrom(amountBytes.ToArray());
            var hashFromUid = Hash.LoadFromByteArray(ByteArrayHelper.HexStringToByteArray(uid));
            var hash = HashHelper.ConcatAndCompute(hashFromAmount, hashFromString, hashFromUid);
            Assert.True(hash == Hash.LoadFromByteArray(ByteArrayHelper.HexStringToByteArray(result)));
        }

        #region Some useful methods

        private List<Hash> CreateLeaves(IEnumerable<string> buffers)
        {
            return buffers.Select(HashHelper.ComputeFrom).ToList();
        }

        private List<Hash> CreateLeaves(int i)
        {
            List<Hash> res = new List<Hash>();
            for (int j = 0; j < i; j++)
            {
                res.Add(HashHelper.ComputeFrom(j.ToString()));
            }

            return res;
        }

        private Hash CreateLeafFromHex(string hex)
        {
            return Hash.LoadFromByteArray(ByteArrayHelper.HexStringToByteArray(hex));
        }

        private Hash ComputeRootWithMerklePathAndLeaf(Hash leaf, MerklePath path)
        {
            return path.ComputeRootWithLeafNode(leaf);
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