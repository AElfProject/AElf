using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.ChainController;
using AElf.SmartContract;

namespace AElf.Kernel.Node
{
    public interface IAElfNode : IBlockExecutor
    {
        bool Start(ECKeyPair nodeKeyPair, bool startRpc, int rpcPort, string rpcHost, string initData, byte[] code);

        List<Hash> GetMissingTransactions(IBlock block);
        Task<BlockExecutionResult> ExecuteAndAddBlock(IBlock block);

        Task ReceiveTransaction(byte[] messagePayload, bool isFromSend);

        Task<ulong> GetCurrentChainHeight();
        
        BlockProducer BlockProducers { get; }
        Hash ContractAccountHash { get; }
        IExecutive Executive { get; }
        
        int IsMiningInProcess { get; }
    }
}