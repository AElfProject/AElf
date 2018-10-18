using System.Collections.Generic;
using Google.Protobuf.Collections;
using AElf.Common;

namespace AElf.Kernel
{
    public interface IBlockBody: IHashProvider
    {
        RepeatedField<Hash> Transactions { get; }

        bool AddTransaction(Transaction tx);

        bool AddTransactions(IEnumerable<Transaction> txs);
        
        void Complete(Hash blockHeaderHash);
        
        RepeatedField<SideChainBlockInfo> IndexedInfo { get; }
    }
}