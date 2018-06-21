using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Miner;

namespace AElf.Kernel.Node
{
    public interface IAElfNode
    {
        void Start(ECKeyPair nodeKeyPair, bool startRpc);

        List<Hash> GetMissingTransactions(IBlock block);
        Task<BlockExecutionResult> AddBlock(IBlock block);

        int GetCurrentChainHeight();
    }
}