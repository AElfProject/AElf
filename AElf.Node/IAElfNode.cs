using System;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Cryptography.ECDSA;
using AElf.Miner.Miner;

namespace AElf.Kernel.Node
{
    public interface IAElfNode
    {
        bool Start(ECKeyPair nodeKeyPair, byte[] tokenContractCode, byte[] consensusGenesisContractCode,
            byte[] basicContractZero);

        Task<BlockExecutionResult> ExecuteAndAddBlock(IBlock block);

        Hash ContractAccountHash { get; }

        int IsMiningInProcess { get; }
    }
}