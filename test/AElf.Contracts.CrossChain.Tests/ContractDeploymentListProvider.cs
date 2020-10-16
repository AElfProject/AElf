using System.Collections.Generic;
using AElf.CrossChain;
using AElf.GovernmentSystem;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;

namespace AElf.Contracts.CrossChain.Tests
{
    public class ContractDeploymentListProvider : IContractDeploymentListProvider
    {
        public List<Hash> GetDeployContractNameList()
        {
            return new List<Hash>
            {
                AssociationSmartContractAddressNameProvider.Name,
                TokenSmartContractAddressNameProvider.Name,
                ParliamentSmartContractAddressNameProvider.Name,
                ConsensusSmartContractAddressNameProvider.Name,
                CrossChainSmartContractAddressNameProvider.Name,
            };
        }
    }
}