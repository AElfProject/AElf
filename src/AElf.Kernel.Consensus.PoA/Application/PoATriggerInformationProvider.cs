using AElf.Kernel.Consensus.Application;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.PoA.Application;

public class PoATriggerInformationProvider : ITriggerInformationProvider, ISingletonDependency
{
    public BytesValue GetTriggerInformationForConsensusCommand(BytesValue consensusCommandBytes)
    {
        return new BytesValue
        {
            Value = ByteString.Empty
        };
    }

    public BytesValue GetTriggerInformationForBlockHeaderExtraData(BytesValue consensusCommandBytes)
    {
        return new BytesValue
        {
            Value = ByteString.Empty
        };
    }

    public BytesValue GetTriggerInformationForConsensusTransactions(IChainContext chainContext, BytesValue consensusCommandBytes)
    {
        return new BytesValue
        {
            Value = ByteString.Empty
        };
    }
}