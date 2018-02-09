using System;
using AElf.Kernel.Extensions;
using AElf.Kernel.Merkle;
using System.Collections.Generic;
using Xunit;

namespace AElf.Kernel.Tests
{
    public class MerkleTest
    {
        [Fact]
        public void VerifyProofListTest()
        {
            BinaryMerkleTree<ITransaction> tree = new BinaryMerkleTree<ITransaction>();
            tree.AddNodes(CreateLeaves(new string[] { "a", "e", "l", "f" }));

            #region Create elements of Proof List
            /* Merkle Tree:
             *                      root
             *  hash(hash(a),hash(e))   hash(hash(l),hash(f))
             *  hash(a)     hash(e)     hash(l)     hash(f)
             *      a        e          l           f
             */
            //Proof List: { hash(a), hash(e), hash(hash(l), hash(f)) }
            var hash_a = new Hash<ITransaction>("a".CalculateHash());

            var hash_e = new Hash<ITransaction>("e".CalculateHash());

            var hash_l = new Hash<ITransaction>("l".CalculateHash());
            var hash_f = new Hash<ITransaction>("f".CalculateHash());
            var hash_l_f = new Hash<ITransaction>(hash_l.CalculateHashWith(hash_f));
            #endregion

            List<IHash<ITransaction>> prooflist = new List<IHash<ITransaction>>
            {
                hash_a,
                hash_e,
                hash_l_f
            };

            Assert.True(tree.VerifyProofList(prooflist));
        }

        #region Some methods
        private static List<IHash<ITransaction>> CreateLeaves(string[] buffers)
        {
            List<IHash<ITransaction>> leaves = new List<IHash<ITransaction>>();
            foreach (var buffer in buffers)
            {
                IHash<ITransaction> hash = new Hash<ITransaction>(buffer.CalculateHash());
                leaves.Add(hash);
            }
            return leaves;
        }
        #endregion
    }
}
