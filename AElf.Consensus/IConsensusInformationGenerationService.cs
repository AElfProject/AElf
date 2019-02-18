using System.Threading.Tasks;

namespace AElf.Consensus
{
    public interface IConsensusInformationGenerationService
    {
        byte[] GenerateExtraInformation();
        byte[] GenerateExtraInformationForTransaction(byte[] consensusInformation, int chainId);
        void UpdateConsensusCommand(byte[] consensusCommand);
    }
}