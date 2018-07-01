using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Miner;
using AElf.Kernel.Types;

namespace AElf.Kernel.Node
{
    public interface IAElfNode
    {
        bool Start(ECKeyPair nodeKeyPair, bool startRpc, int confParserRpcPort, string initData, byte[] code);

        List<Hash> GetMissingTransactions(IBlock block);
        Task<BlockExecutionResult> ExecuteAndAddBlock(IBlock block);

        Task<ulong> GetCurrentChainHeight();
        
        BlockProducer BlockProducers { get; }
        Hash ContractAccountHash { get; }
        IExecutive Executive { get; }
        
        int IsMiningInProcess { get; }
    }
}