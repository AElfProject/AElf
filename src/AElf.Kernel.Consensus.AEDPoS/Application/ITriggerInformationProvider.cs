using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public interface ITriggerInformationProvider
    {
        BytesValue GetTriggerInformationToGetConsensusCommand();
        BytesValue GetTriggerInformationToGetExtraData();
        BytesValue GetTriggerInformationToGenerateConsensusTransactions();
    }
}