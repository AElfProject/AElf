using System;
using AElf.Kernel;
using AElf.Common;
using Google.Protobuf.WellKnownTypes;

namespace AElf.SmartContract
{
    public interface ITransactionContext
    {
        Hash Origin { get; set; }
        Hash Miner { get; set; }
        Hash PreviousBlockHash { get; set; }
        ulong BlockHeight { get; set;}
        
        DateTime CurrentBlockTime { get; set; }
        
        int CallDepth { get; set; }
        Transaction Transaction { get; set; }
        TransactionTrace Trace { get; set; }
    }
}
