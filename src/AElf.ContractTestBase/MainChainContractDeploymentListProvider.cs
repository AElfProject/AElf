using System.Collections.Generic;
using AElf.CrossChain;
using AElf.EconomicSystem;
using AElf.GovernmentSystem;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContractInitialization;
using AElf.Kernel.Token;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.ContractTestBase
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
                CrossChainSmartContractAddressNameProvider.Name,
                ConfigurationSmartContractAddressNameProvider.Name,
                ConsensusSmartContractAddressNameProvider.Name,
                TokenConverterSmartContractAddressNameProvider.Name,
                TokenHolderSmartContractAddressNameProvider.Name,
                EconomicSmartContractAddressNameProvider.Name,
                ReferendumSmartContractAddressNameProvider.Name
            };
        }
    }
}