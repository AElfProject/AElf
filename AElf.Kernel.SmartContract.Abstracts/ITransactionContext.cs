using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Common;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract
{
    public interface ITransactionContext
    {
        Address Origin { get; set; }
        Address Miner { get; set; }
        Hash PreviousBlockHash { get; set; }
        ulong BlockHeight { get; set;}
        
        DateTime CurrentBlockTime { get; set; }
        
        int CallDepth { get; set; }
        Transaction Transaction { get; set; }
        TransactionTrace Trace { get; set; }
        
        Task<Block> GetBlockByHashAsync(int chainId, Hash blockId);

    }
}
