using AElf.Contracts.Consensus.AEDPoS;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    internal interface ITriggerInformationProvider
    {
        CommandInput GetTriggerInformationToGetConsensusCommand();
        AElfConsensusTriggerInformation GetTriggerInformationToGetExtraData();
        AElfConsensusTriggerInformation GetTriggerInformationToGenerateConsensusTransactions();
    }
}