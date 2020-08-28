using System.Collections.Generic;
using AElf.CrossChain;
using AElf.GovernmentSystem;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;

namespace AElf.Contracts.MultiToken
{
    public class MultiChainContractDeploymentListProvider : IContractDeploymentListProvider
    {
        public List<Hash> GetDeployContractNameList()
        {
            return new List<Hash>
            {
                ConsensusSmartContractAddressNameProvider.Name,
                TokenSmartContractAddressNameProvider.Name,
                ParliamentSmartContractAddressNameProvider.Name,
                CrossChainSmartContractAddressNameProvider.Name,
                AssociationSmartContractAddressNameProvider.Name,
                ReferendumSmartContractAddressNameProvider.Name
            };
        }
    }
    
    public class SideChainContractDeploymentListProvider : IContractDeploymentListProvider
    {
        public List<Hash> GetDeployContractNameList()
        {
            return new List<Hash>
            {
                ConsensusSmartContractAddressNameProvider.Name,
                ParliamentSmartContractAddressNameProvider.Name,
                ReferendumSmartContractAddressNameProvider.Name,
                AssociationSmartContractAddressNameProvider.Name,
                TokenSmartContractAddressNameProvider.Name,
                CrossChainSmartContractAddressNameProvider.Name
            };
        }
    }
}