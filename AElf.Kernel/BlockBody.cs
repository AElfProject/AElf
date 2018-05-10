using AElf.Kernel.Merkle;

namespace AElf.Kernel
{
    public partial class BlockBody : IBlockBody
    {
        public int TransactionsCount => Transactions.Count;

        public bool AddTransaction(Hash tx)
        {
            if (Transactions.Contains(tx))
                return false;
            
            Transactions.Add(tx);
            
            return true;
        }

        public Hash CalculateMerkleTreeRoot()
        {
            var merkleTree = new BinaryMerkleTree();
            merkleTree.AddNodes(transactions_);
            
            return merkleTree.ComputeRootHash();
        }
    }
}