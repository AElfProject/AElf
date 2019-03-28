﻿using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using Xunit;

namespace AElf.Kernel.Types.Tests
{
    /// <summary>
    ///     Use Transaction to be the type of the merkle node.
    /// </summary>
    public class MerkleTest
    {
        private Hash GetHashFromStrings(params string[] strings)
        {
            var hash = Hash.FromString(strings[0]);
            foreach (var s in strings.Skip(1)) hash = Hash.FromTwoHashes(hash, Hash.FromString(s));

            return hash;
        }

        private List<Hash> CreateLeaves(IEnumerable<string> buffers)
        {
            return buffers.Select(Hash.FromString).ToList();
        }

        private List<Hash> CreateLeaves(int i)
        {
            var res = new List<Hash>();
            for (var j = 0; j < i; j++) res.Add(Hash.Generate());

            return res;
        }

        private Hash CreateLeaf(string buffer)
        {
            return Hash.FromString(buffer);
        }

        private Hash ComputeMerklePath(Hash leaf, MerklePath path)
        {
            return path.ComputeRootWith(leaf);
        }

        [Fact]
        public void MerklePathTest()
        {
            var leaveCount = new Random().Next(100) + 1;
            var hashes = CreateLeaves(leaveCount);
            var bmt = new BinaryMerkleTree();
            bmt.AddNodes(hashes);
            var root = bmt.ComputeRootHash();
            var index = new Random().Next(leaveCount);
            var path = bmt.GenerateMerklePath(index);
            var calculatedRoot = path.ComputeRootWith(hashes[index]);
            Assert.Equal(root, calculatedRoot);
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
            Assert.True(1 == path.Path.Count);
            var realPath = new List<Hash> {tree.Nodes[1]};
            Assert.Equal(realPath, path.Path.ToList());
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
            Assert.True(1 == path.Path.Count);
            var realPath = new List<Hash> {tree.Nodes[1]};
            Assert.Equal(realPath, path.Path.ToList());

            // test "e"
            path = tree.GenerateMerklePath(1);
            Assert.NotNull(path);
            Assert.True(1 == path.Path.Count);
            realPath = new List<Hash> {tree.Nodes[0]};
            Assert.Equal(realPath, path.Path.ToList());
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
            Assert.Equal(3, path.Path.Count);
            var realPath = new List<Hash> {tree.Nodes[1], tree.Nodes[7], tree.Nodes[11]};
            Assert.Equal(realPath, path.Path.ToList());
            var calroot = ComputeMerklePath(Hash.FromString("a"), path);
            Assert.Equal(root, calroot);

            // test 1st "e"
            path = tree.GenerateMerklePath(1);
            Assert.NotNull(path);
            Assert.Equal(3, path.Path.Count);
            realPath = new List<Hash> {tree.Nodes[0], tree.Nodes[7], tree.Nodes[11]};
            Assert.Equal(realPath, path.Path.ToList());
            calroot = ComputeMerklePath(Hash.FromString("e"), path);
            Assert.Equal(root, calroot);

            // test 1st "l"
            path = tree.GenerateMerklePath(2);
            Assert.NotNull(path);
            Assert.Equal(3, path.Path.Count);
            realPath = new List<Hash> {tree.Nodes[3], tree.Nodes[6], tree.Nodes[11]};
            Assert.Equal(realPath, path.Path.ToList());
            calroot = ComputeMerklePath(Hash.FromString("l"), path);
            Assert.Equal(root, calroot);

            // test "f"
            path = tree.GenerateMerklePath(3);
            Assert.NotNull(path);
            Assert.Equal(3, path.Path.Count);
            realPath = new List<Hash> {tree.Nodes[2], tree.Nodes[6], tree.Nodes[11]};
            Assert.Equal(realPath, path.Path.ToList());
            calroot = ComputeMerklePath(Hash.FromString("f"), path);
            Assert.Equal(root, calroot);

            // test 2nd "a"
            path = tree.GenerateMerklePath(4);
            Assert.NotNull(path);
            Assert.Equal(3, path.Path.Count);
            realPath = new List<Hash> {tree.Nodes[5], tree.Nodes[9], tree.Nodes[10]};
            Assert.Equal(realPath, path.Path.ToList());
            calroot = ComputeMerklePath(Hash.FromString("a"), path);
            Assert.Equal(root, calroot);

            // test invalid idnex
            path = tree.GenerateMerklePath(5);
            Assert.Null(path);
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
            var right = GetHashFromStrings("l", "l");
            Assert.Equal(Hash.FromTwoHashes(root1, right), root2);

            var tree3 = new BinaryMerkleTree();
            tree3.AddNodes(CreateLeaves(new[] {"a", "e", "l", "f"}));
            var root3 = tree3.ComputeRootHash();
            var right2 = GetHashFromStrings("l", "f");
            Assert.Equal(Hash.FromTwoHashes(root1, right2), root3);

            var tree4 = new BinaryMerkleTree();
            tree4.AddNodes(CreateLeaves(new[] {"a", "e", "l", "f", "a"}));
            var root4 = tree4.ComputeRootHash();
            var l2 = GetHashFromStrings("a", "a");
            var l3 = Hash.FromTwoHashes(l2, l2);
            Assert.Equal(Hash.FromTwoHashes(root3, l3), root4);
        }

        /// <summary>
        ///     Add node(s) and compute root hash
        /// </summary>
        [Fact]
        public void SingleNodeTest()
        {
            var tree = new BinaryMerkleTree();

            tree.AddNode(CreateLeaf("a"));

            //See if the hash of merkle tree is equal to the element’s hash.
            var root = tree.ComputeRootHash();
            Assert.Equal(GetHashFromStrings("a", "a"), root);

            var b = tree.AddNode(CreateLeaf("e"));

            Assert.False(b);
        }
    }
}