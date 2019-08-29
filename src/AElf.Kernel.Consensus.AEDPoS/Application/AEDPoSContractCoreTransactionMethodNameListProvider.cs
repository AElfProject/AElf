using System.Collections.Generic;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Consensus.Application;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public class AEDPoSContractCoreTransactionMethodNameListProvider : IConsensusCoreTransactionMethodNameListProvider
    {
        public List<string> GetCoreTransactionMethodNameList()
        {
            return new List<string>
            {
                nameof(AEDPoSContractContainer.AEDPoSContractStub.InitialAElfConsensusContract),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.FirstRound),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.NextRound),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.NextTerm),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.UpdateValue),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.UpdateTinyBlockInformation)
            };
        }
    }
}