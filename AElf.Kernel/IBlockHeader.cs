using System;
using System.Transactions;

namespace AElf.Kernel
{
    public interface IBlockHeader
    {
        IHash<IMerkleTree<ITransaction>> GetTransactionMerkleTreeRoot();
        void AddTransaction(IHash<ITransaction> hash);
    }
}