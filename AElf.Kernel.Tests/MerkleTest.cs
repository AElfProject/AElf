using AElf.Kernel.Extensions;
using System.Collections.Generic;
using Xunit;

namespace AElf.Kernel.Tests
{
    public class MerkleTest
    {
        [Fact]
        public void VerifyProofListTest()
        {
            MerkleTree<ITransaction> tree = new MerkleTree<ITransaction>();
            tree.AddNodes(CreateLeaves(new string[] { "a", "e", "l", "f" }));

            #region Create elements of Proof List
            /* Merkle Tree:
             *                      root
             *  hash(hash(a),hash(e))   hash(hash(l),hash(f))
             *  hash(a)     hash(e)     hash(l)     hash(f)
             *      a        e          l           f
             */
            //Proof List: { hash(a), hash(e), hash(hash(l), hash(f)) }
            var hash_a = new Hash<ITransaction>("a".GetSHA256Hash());

            var hash_e = new Hash<ITransaction>("e".GetSHA256Hash());

            var hash_l = new Hash<ITransaction>("l".GetSHA256Hash());
            var hash_f = new Hash<ITransaction>("f".GetSHA256Hash());
            var hash_l_f = new Hash<ITransaction>((hash_l.ToString() + hash_f.ToString()).GetSHA256Hash());
            #endregion

            List<Hash<ITransaction>> prooflist = new List<Hash<ITransaction>>
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
                IHash<ITransaction> hash = new Hash<ITransaction>(buffer.GetSHA256Hash());
                leaves.Add(hash);
            }
            return leaves;
        }
        #endregion
    }
}
