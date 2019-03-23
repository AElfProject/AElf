using AElf.Consensus.DPoS;
using Google.Protobuf;

namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusInformationGenerationService
    {
        DPoSTriggerInformation GetTriggerInformation();
        IMessage ParseConsensusTriggerInformation(byte[] consensusTriggerInformation);
    }
}