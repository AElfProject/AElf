using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus.Application
{
    public interface ITriggerInformationProvider
    {
        BytesValue GetTriggerInformationForConsensusCommand();
        BytesValue GetTriggerInformationForBlockHeaderExtraData();
        BytesValue GetTriggerInformationForConsensusTransactions();
    }
}