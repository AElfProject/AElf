using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

namespace AElf.Consensus
{
    public interface IConsensusService
    {
        Task TriggerConsensusAsync(int chainId);

        Task<bool> ValidateConsensusAsync(int chainId, Hash preBlockHash, ulong preBlockHeight,
            byte[] consensusInformation);
        Task<byte[]> GetNewConsensusInformationAsync(int chainId);
        Task<IEnumerable<Transaction>> GenerateConsensusTransactionsAsync(int chainId, ulong refBlockHeight, byte[] refBlockPrefix);
    }
}