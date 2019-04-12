using System;
using AElf.Common;

namespace AElf.Kernel.SmartContract.Sdk
{
    public interface ITransactionContext
    {
        Address Origin { get; set; }
        Address Miner { get; set; }
        Hash PreviousBlockHash { get; set; }
        long BlockHeight { get; set;}
        
        DateTime CurrentBlockTime { get; set; }
        
        int CallDepth { get; set; }
        int MaxCallDepth { get; set; }
        Transaction Transaction { get; set; }
        TransactionTrace Trace { get; set; }
        IStateCache StateCache { get; set; }

    }
}
