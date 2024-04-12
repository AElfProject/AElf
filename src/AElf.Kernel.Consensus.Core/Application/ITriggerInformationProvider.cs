using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus.Application;

// ReSharper disable UnusedParameter.Global
public interface ITriggerInformationProvider
{
    BytesValue GetTriggerInformationForConsensusCommand(BytesValue consensusCommandBytes);
    BytesValue GetTriggerInformationForBlockHeaderExtraData(BytesValue consensusCommandBytes);
    BytesValue GetTriggerInformationForConsensusTransactions(IChainContext chainContext, BytesValue consensusCommandBytes);
}