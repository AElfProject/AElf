using System.Collections.Generic;
using AElf.CrossChain;
using AElf.EconomicSystem;
using AElf.GovernmentSystem;
using AElf.Kernel.Configuration;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;

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
                ParliamentSmartContractAddressNameProvider.Name,
                AssociationSmartContractAddressNameProvider.Name,
                ReferendumSmartContractAddressNameProvider.Name,
                TokenSmartContractAddressNameProvider.Name,
                CrossChainSmartContractAddressNameProvider.Name,
                ConfigurationSmartContractAddressNameProvider.Name,
                ConsensusSmartContractAddressNameProvider.Name,
                TokenConverterSmartContractAddressNameProvider.Name,
                TokenHolderSmartContractAddressNameProvider.Name,
                EconomicSmartContractAddressNameProvider.Name,
            };
        }
    }
}