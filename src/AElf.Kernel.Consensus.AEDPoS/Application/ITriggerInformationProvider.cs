using Google.Protobuf;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public interface ITriggerInformationProvider
    {
        IMessage GetTriggerInformationToGetConsensusCommand();
        IMessage GetTriggerInformationToGetExtraData();
        IMessage GetTriggerInformationToGenerateConsensusTransactions();
    }
}