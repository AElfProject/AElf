using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusService
    {
        Task TriggerConsensusAsync();

        Task<bool> ValidateConsensusAsync(Hash preBlockHash, long preBlockHeight,
            byte[] consensusInformation);
        Task<byte[]> GetNewConsensusInformationAsync();
        Task<IEnumerable<Transaction>> GenerateConsensusTransactionsAsync(long refBlockHeight, byte[] refBlockPrefix);
    }
}