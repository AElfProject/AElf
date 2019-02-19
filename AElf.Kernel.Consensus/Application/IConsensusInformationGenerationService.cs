namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusInformationGenerationService
    {
        byte[] GenerateExtraInformation();
        byte[] GenerateExtraInformationForTransaction(byte[] consensusInformation, int chainId);
    }
}