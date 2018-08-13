using System;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Cryptography.ECDSA;

namespace AElf.Kernel.Node
{
    public interface IAElfNode
    {
        bool Start(ECKeyPair nodeKeyPair, bool startRpc, int rpcPort, string rpcHost, string initData, byte[] code,
            byte[] consensusGenesisContractCode, byte[] basicContractZero);

        Task<BlockExecutionResult> ExecuteAndAddBlock(IBlock block);

//        Task ReceiveTransaction(byte[] messagePayload, bool isFromSend);

//        BlockProducer BlockProducers { get; }

        Hash ContractAccountHash { get; }

        int IsMiningInProcess { get; }
    }
}