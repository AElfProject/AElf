using Google.Protobuf;

namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusInformationGenerationService
    {
        IMessage GetTriggerInformation();
        IMessage ParseConsensusTriggerInformation(byte[] consensusTriggerInformation);
    }
}