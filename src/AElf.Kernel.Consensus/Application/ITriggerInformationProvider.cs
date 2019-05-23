using Acs4;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus.Application
{
    public interface ITriggerInformationProvider
    {
        BytesValue GetTriggerInformationForConsensusCommand(BytesValue consensusCommandBytes);
        BytesValue GetTriggerInformationForBlockHeaderExtraData(BytesValue consensusCommandBytes);
        BytesValue GetTriggerInformationForConsensusTransactions(BytesValue consensusCommandBytes);
    }
}