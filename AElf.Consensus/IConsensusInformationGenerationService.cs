using System.Threading.Tasks;

namespace AElf.Consensus
{
    public interface IConsensusInformationGenerationService
    {
        byte[] GenerateExtraInformationAsync();
        byte[] GenerateExtraInformationForTransactionAsync(byte[] consensusInformation, int chainId);
        void UpdateConsensusCommand(byte[] consensusCommand);
    }
}