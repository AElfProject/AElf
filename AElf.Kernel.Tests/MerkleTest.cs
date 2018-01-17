﻿using System.Collections.Generic;
using Xunit;

namespace AElf.Kernel.Tests
{
    public class MerkleTest
    {

        //[Fact]
        //public void ProofListTest()
        //{
        //    MerkleTree tree = new MerkleTree();
        //    tree.AddLeaves(CreateLeaves(new string[] { "a", "e", "l", "f", "2", "0", "1", "8" }))
        //        .Generate();

        //    Hash target = new Hash("e");
        //    var prooflist = tree.GetProofList(target);

        //    Assert.True(prooflist[0].Hash.ToString() == new Hash("a").ToString());
        //    Assert.True(prooflist[prooflist.Count - 1].Hash.ToString() == tree.MerkleRoot.Hash.ToString());
        //}

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

        //private static MerkleNode CreateNode(string buffer1, string buffer2)
        //{
        //    MerkleNode left = new MerkleNode
        //    {
        //        Hash = new Hash<T>(buffer1)
        //    };
        //    MerkleNode right = new MerkleNode
        //    {
        //        Hash = new Hash<T>(buffer2)
        //    };

        //    MerkleNode parent = new MerkleNode();
        //    parent.SetLeftNode(left);
        //    parent.SetRightNode(right);

        //    return parent;
        //}

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
