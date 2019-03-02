using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusService
    {
        Task TriggerConsensusAsync();

        Task<bool> ValidateConsensusAsync(Hash preBlockHash, ulong preBlockHeight,
            byte[] consensusInformation);
        Task<byte[]> GetNewConsensusInformationAsync();
        Task<IEnumerable<Transaction>> GenerateConsensusTransactionsAsync(ulong refBlockHeight, byte[] refBlockPrefix);
    }
}