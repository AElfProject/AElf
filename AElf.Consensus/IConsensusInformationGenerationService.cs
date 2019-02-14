using System.Threading.Tasks;

namespace AElf.Consensus
{
    public interface IConsensusInformationGenerationService
    {
        Task<byte[]> GenerateExtraInformationAsync(byte[] consensusInformation);
        Task<byte[]> GenerateExtraInformationForTransactionAsync(byte[] consensusInformation);
        void Tell(byte[] consensusCommand);
    }
}