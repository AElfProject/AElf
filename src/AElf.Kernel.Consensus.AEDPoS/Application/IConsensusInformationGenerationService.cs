using System;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    // TODO: Refactor.
    public interface IConsensusInformationGenerationService
    {
        IMessage GetTriggerInformation(TriggerType triggerType);

        IMessage ParseConsensusTriggerInformation(byte[] consensusTriggerInformation);

        Task<T> ExecuteContractAsync<T>(IChainContext chainContext, string consensusMethodName,
            IMessage input, Timestamp dateTime) where T : class, IMessage<T>, new();

        Task<byte[]> GetInformationToUpdateConsensusAsync(ChainContext chainContext, Timestamp nextMiningTime);
    }

    public enum TriggerType
    {
        ConsensusCommand,
        BlockHeaderExtraData,
        ConsensusTransactions
    }
}