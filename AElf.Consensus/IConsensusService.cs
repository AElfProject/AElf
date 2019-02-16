using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

namespace AElf.Consensus
{
    public interface IConsensusService
    {
        Task StartConsensus(int chainId);
        Task StopConsensus();
        Task<bool> ValidateConsensus(int chainId, byte[] consensusInformation);
        Task<byte[]> GetNewConsensusInformation(int chainId);
        Task<IEnumerable<Transaction>> GenerateConsensusTransactions(int chainId, ulong refBlockHeight, byte[] refBlockPrefix);
    }
}