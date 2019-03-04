namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusInformationGenerationService
    {
        byte[] GetTriggerInformation();
        byte[] GenerateExtraInformation();
        byte[] GenerateExtraInformationForTransaction(byte[] consensusInformation);
    }
}