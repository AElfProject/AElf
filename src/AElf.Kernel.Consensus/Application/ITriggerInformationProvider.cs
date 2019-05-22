using Acs4;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus.Application
{
    public interface ITriggerInformationProvider
    {
        BytesValue GetTriggerInformationForConsensusCommand(BytesValue consensusCommand);
        BytesValue GetTriggerInformationForBlockHeaderExtraData(BytesValue consensusCommand);
        BytesValue GetTriggerInformationForConsensusTransactions(BytesValue consensusCommandBytes);
    }
}