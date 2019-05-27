using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus.Application
{
    public interface ITriggerInformationProvider
    {
        BytesValue GetTriggerInformationForConsensusCommand(BytesValue consensusCommandBytes);
        Task<BytesValue> GetTriggerInformationForBlockHeaderExtraDataAsync(BytesValue consensusCommandBytes);
        Task<BytesValue> GetTriggerInformationForConsensusTransactionsAsync(BytesValue consensusCommandBytes);
    }
}