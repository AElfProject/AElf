using System.Collections.Generic;
using System.Linq;
using AElf.Kernel.Merkle;

namespace AElf.Kernel
{
    public partial class BlockBody : IBlockBody
    {

        public int TransactionsCount => transactions_.Count;


        public bool AddTransaction(Hash tx)
        {
            
            if (transactions_.Contains(tx))
                return false;
            transactions_.Add(tx);
            return true;
        }

        public Hash CalculateMerkleTreeRoot()
        {
            Merkle.BinaryMerkleTree merkleTree=new BinaryMerkleTree();
            merkleTree.AddNodes(transactions_);
            return merkleTree.ComputeRootHash();
        }
    }
}