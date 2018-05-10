using AElf.Kernel.Extensions;
using AElf.Kernel.Merkle;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AElf.Kernel.Tests
{
    /// <summary>
    /// Use ITransaction to be the type of the merkle node.
    /// </summary>
    public class MerkleTest
    {
        /// <summary>
        /// Add node(s) and compute root hash
        /// </summary>
        [Fact]
        public void AddNodeTest()
        {
            var tree = new BinaryMerkleTree();
            
            tree.AddNode(CreateLeaf("a"));

            //See if the hash of merkle tree is equal to the element’s hash.
            Assert.True(tree.ComputeRootHash().Equals(
                        new Hash("a".CalculateHash())));
            
            tree.AddNode(CreateLeaf("e"));

            var hash_a = new Hash("a".CalculateHash());
            var hash_e = new Hash("e".CalculateHash());

            //See if the hash of merkle tree is equal to the elements' hash.
            Assert.True(tree.ComputeRootHash().Equals(
                        new Hash(hash_a.CalculateHashWith(hash_e))));
        }
        
        /// <summary>
        /// Add four nodes, record root hash, update one node,
        /// then compare current root hash to before.
        /// </summary>
        [Fact]
        public void UpdateNodeTest()
        {
            var tree = new BinaryMerkleTree();
            
            tree.AddNodes(CreateLeaves(new[] { "a", "e", "l", "f" }));

            var hashBeforeUpdate = tree.ComputeRootHash();
            
            var hash_l = new Hash("l".CalculateHash());
            var hash_1 = new Hash("1".CalculateHash());

            tree.UpdateNode(hash_l, hash_1);

            var hashAfterUpdate = tree.ComputeRootHash();
            
            Assert.True(!hashAfterUpdate.Equals(hashBeforeUpdate));
        }

        /// <summary>
        /// Add four nodes, then find one of nodes added before.
        /// </summary>
        [Fact]
        public void FindNodeTest()
        {
            var tree = new BinaryMerkleTree();
            
            tree.AddNodes(CreateLeaves(new[] { "a", "e", "l", "f" }));

            var hash_l = new Hash("l".CalculateHash());
            var hash_1 = new Hash("1".CalculateHash());
            
            Assert.True(tree.FindLeaf(hash_l) > -1);
            Assert.True(tree.FindLeaf(hash_1) == -1);
        }
        
        /// <summary>
        /// Add four nodes, then create a proof list, verify the proof list.
        /// </summary>
        [Fact]
        public void VerifyProofListTest()
        {
            var tree = new BinaryMerkleTree();
            
            tree.AddNodes(CreateLeaves(new[] { "a", "e", "l", "f" }));

            #region Create elements of Proof List
            /* Merkle Tree:
             *                      root
             *  hash(hash(a),hash(e))   hash(hash(l),hash(f))
             *  hash(a)     hash(e)     hash(l)     hash(f)
             *      a        e          l           f
             */
            //Proof List: { hash(a), hash(e), hash(hash(l), hash(f)) }
            var hash_a = new Hash("a".CalculateHash());

            var hash_e = new Hash("e".CalculateHash());

            var hash_l = new Hash("l".CalculateHash());
            var hash_f = new Hash("f".CalculateHash());
            var hash_l_f = new Hash(hash_l.CalculateHashWith(hash_f));
            #endregion

            //Construct a proof list.
            var prooflist = new List<Hash>
            {
                hash_a,
                hash_e,
                hash_l_f
            };
            
            //Do the proof list verification.
            Assert.True(tree.VerifyProofList(prooflist));
        }

        #region Some useful methods
        private List<Hash> CreateLeaves(IEnumerable<string> buffers)
        {
            return buffers.Select(buffer => new Hash(buffer.CalculateHash())).Cast<Hash>().ToList();
        }

        private Hash CreateLeaf(string buffer)
        {
            return new Hash(buffer.CalculateHash());
        }
        #endregion
    }
}
