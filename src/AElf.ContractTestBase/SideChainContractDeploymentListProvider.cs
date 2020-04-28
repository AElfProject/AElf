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

namespace AElf.ContractTestBase
{
    public class SideChainContractDeploymentListProvider : IContractDeploymentListProvider
    {
        public List<Hash> GetDeployContractNameList()
        {
            return new List<Hash>
            {
                ProfitSmartContractAddressNameProvider.Name,                
                TokenHolderSmartContractAddressNameProvider.Name,
                ConsensusSmartContractAddressNameProvider.Name,
                AssociationSmartContractAddressNameProvider.Name,
                ReferendumSmartContractAddressNameProvider.Name,
                ParliamentSmartContractAddressNameProvider.Name,
                TokenSmartContractAddressNameProvider.Name,
                CrossChainSmartContractAddressNameProvider.Name,
                ConfigurationSmartContractAddressNameProvider.Name
            };
        }
    }
}