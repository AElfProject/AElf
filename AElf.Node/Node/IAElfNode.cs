using System;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Cryptography.ECDSA;

namespace AElf.Kernel.Node
{
    public interface IAElfNode
    {
        bool Start(ECKeyPair nodeKeyPair, bool startRpc, int rpcPort, string rpcHost, string initData, byte[] code);

        Task<BlockExecutionResult> ExecuteAndAddBlock(IBlock block);

        Task ReceiveTransaction(byte[] messagePayload, bool isFromSend);

        Task<ulong> GetCurrentChainHeight();

        BlockProducer BlockProducers { get; }

        Hash ContractAccountHash { get; }

        IDisposable ConsensusDisposable { get; set; }

        ulong CurrentRoundNumber { get; set; }

        // ReSharper disable once InconsistentNaming
        void CheckUpdatingDPoSProcess();

        int IsMiningInProcess { get; }
    }
}