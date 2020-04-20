using System.Collections.Generic;
using AElf.EconomicSystem;
using AElf.GovernmentSystem;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.ContractsInitialization;
using AElf.Kernel.Proposal;
using AElf.Kernel.Token;
using AElf.Types;

namespace AElf.Blockchains.MainChain
{
    public class MainChainContractDeploymentListProvider : IContractDeploymentListProvider
    {
        public List<Hash> GetDeployContractNameList()
        {
            return new List<Hash>
            {
                VoteSmartContractAddressNameProvider.Name,
                ProfitSmartContractAddressNameProvider.Name,
                ElectionSmartContractAddressNameProvider.Name,
                TreasurySmartContractAddressNameProvider.Name,
                TokenSmartContractAddressNameProvider.Name,
                ParliamentSmartContractAddressNameProvider.Name,
                AssociationSmartContractAddressNameProvider.Name,
                ConfigurationSmartContractAddressNameProvider.Name,
                ConsensusSmartContractAddressNameProvider.Name,
                TokenConverterSmartContractAddressNameProvider.Name,
                TokenHolderSmartContractAddressNameProvider.Name,
                ReferendumSmartContractAddressNameProvider.Name
            };
        }
    }
}