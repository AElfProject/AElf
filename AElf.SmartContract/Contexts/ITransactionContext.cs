using AElf.Kernel;
using AElf.Common;

namespace AElf.SmartContract
{
    public interface ITransactionContext
    {
        Hash Origin { get; set; }
        Hash Miner { get; set; }
        Hash PreviousBlockHash { get; set; }
        ulong BlockHeight { get; set;}
        int CallDepth { get; set; }
        Transaction Transaction { get; set; }
        TransactionTrace Trace { get; set; }
    }
}
