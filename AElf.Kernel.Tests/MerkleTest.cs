using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AElf.Kernel.Tests
{
    /// <summary>
    /// Use Transaction to be the type of the merkle node.
    /// </summary>
    public class MerkleTest
    {
        /// <summary>
        /// Add node(s) and compute root hash
        /// </summary>
        [Fact]
        public void SingleNodeTest()
        {
            var tree = new BinaryMerkleTree();
            
            tree.AddNode(CreateLeaf("a"));

            //See if the hash of merkle tree is equal to the element’s hash.
            var root = tree.ComputeRootHash();
            Assert.Equal(new Hash("a".CalculateHash()).CalculateWith(new Hash("a".CalculateHash())), root);
            
            var b = tree.AddNode(CreateLeaf("e"));

            Assert.False(b);
        }

        [Fact]
        public void MultiNodesTest()
        {
            var tree1 = new BinaryMerkleTree();
            tree1.AddNodes(CreateLeaves(new[] { "a", "e" }));

            //See if the hash of merkle tree is equal to the element’s hash.
            var root1 = tree1.ComputeRootHash();
            Assert.Equal(new Hash("a".CalculateHash()).CalculateWith(new Hash("e".CalculateHash())), root1);
            
            var tree2 = new BinaryMerkleTree();
            tree2.AddNodes(CreateLeaves(new[] { "a", "e" , "l"}));
            var root2 = tree2.ComputeRootHash();
            Hash right = new Hash("l".CalculateHash()).CalculateWith(new Hash("l".CalculateHash()));
            Assert.Equal(root1.CalculateWith(right), root2);
            
            var tree3 = new BinaryMerkleTree();
            tree3.AddNodes(CreateLeaves(new[] { "a", "e" , "l", "f"}));
            var root3 = tree3.ComputeRootHash();
            Hash right2 = new Hash("l".CalculateHash()).CalculateWith(new Hash("f".CalculateHash()));
            Assert.Equal(root1.CalculateWith(right2), root3);
            
            var tree4 = new BinaryMerkleTree();
            tree4.AddNodes(CreateLeaves(new[] {"a", "e", "l", "f", "a"}));
            var root4 = tree4.ComputeRootHash();
            Hash l2 = new Hash("a".CalculateHash()).CalculateWith(new Hash("a".CalculateHash()));
            Hash l3 = l2.CalculateWith(l2);
            Assert.Equal(root3.CalculateWith(l3), root4);
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
            Assert.Equal(1, path.Path.Count);
            var realPath = new List<Hash>{tree.Nodes[1]};
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
            Assert.Equal(1, path.Path.Count);
            var realPath = new List<Hash>{tree.Nodes[1]};
            Assert.Equal(realPath, path.Path.ToList());
            
            // test "e"
            path = tree.GenerateMerklePath(1);
            Assert.NotNull(path);
            Assert.Equal(1, path.Path.Count);
            realPath = new List<Hash>{tree.Nodes[0]};
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
            var realPath = new List<Hash>{tree.Nodes[1], tree.Nodes[7], tree.Nodes[11]};
            Assert.Equal(realPath, path.Path.ToList());
            var calroot = ComputeMerklePath(new Hash("a".CalculateHash()), path);
            Assert.Equal(root, calroot);
            
            // test 1st "e"
            path = tree.GenerateMerklePath(1);
            Assert.NotNull(path);
            Assert.Equal(3, path.Path.Count);
            realPath = new List<Hash>{tree.Nodes[0], tree.Nodes[7], tree.Nodes[11]};
            Assert.Equal(realPath, path.Path.ToList());
            calroot = ComputeMerklePath(new Hash("e".CalculateHash()), path);
            Assert.Equal(root, calroot);
            
            // test 1st "l"
            path = tree.GenerateMerklePath(2);
            Assert.NotNull(path);
            Assert.Equal(3, path.Path.Count);
            realPath = new List<Hash>{tree.Nodes[3], tree.Nodes[6], tree.Nodes[11]};
            Assert.Equal(realPath, path.Path.ToList());
            calroot = ComputeMerklePath(new Hash("l".CalculateHash()), path);
            Assert.Equal(root, calroot);
            
            // test "f"
            path = tree.GenerateMerklePath(3);
            Assert.NotNull(path);
            Assert.Equal(3, path.Path.Count);
            realPath = new List<Hash>{tree.Nodes[2], tree.Nodes[6], tree.Nodes[11]};
            Assert.Equal(realPath, path.Path.ToList());
            calroot = ComputeMerklePath(new Hash("f".CalculateHash()), path);
            Assert.Equal(root, calroot);
            
            // test 2nd "a"
            path = tree.GenerateMerklePath(4);
            Assert.NotNull(path);
            Assert.Equal(3, path.Path.Count);
            realPath = new List<Hash>{tree.Nodes[5], tree.Nodes[9], tree.Nodes[10]};
            Assert.Equal(realPath, path.Path.ToList());
            calroot = ComputeMerklePath(new Hash("a".CalculateHash()), path);
            Assert.Equal(root, calroot);
            
            // test invalid idnex
            path = tree.GenerateMerklePath(5);
            Assert.Null(path);
        }
        
        
       #region Some useful methods
        private List<Hash> CreateLeaves(IEnumerable<string> buffers)
        {
            return buffers.Select(buffer => new Hash(buffer.CalculateHash())).ToList();
        }

        private Hash CreateLeaf(string buffer)
        {
            return new Hash(buffer.CalculateHash());
        }

        private Hash ComputeMerklePath(Hash leaf, MerklePath path)
        {
            return path.ComputeRootWith(leaf);
        }
        #endregion
    }
}
