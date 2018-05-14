using AElf.Kernel.Merkle;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
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

        public byte[] Serialize()
        {
            return this.ToByteArray();
        }

        public Hash GetHash()
        {
            throw new System.NotImplementedException();
        }
    }
}