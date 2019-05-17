using System;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using Google.Protobuf;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    internal interface IConsensusInformationGenerationService
    {

        AElfConsensusTriggerInformation ParseConsensusTriggerInformation(byte[] consensusTriggerInformation);
        AElfConsensusHeaderInformation ParseHeaderExtraData(byte[] consensusTriggerInformation);

        Task<T> ExecuteContractAsync<T>(IChainContext chainContext, string consensusMethodName,
            IMessage input, DateTime dateTime) where T : class, IMessage<T>, new();

        Task<byte[]> GetInformationToUpdateConsensusAsync(ChainContext chainContext, DateTime nextMiningTime);
    }

    public enum TriggerType
    {
        ConsensusCommand,
        BlockHeaderExtraData,
        ConsensusTransactions
    }
}