using System.Collections.Generic;
using AElf.CrossChain;
using AElf.EconomicSystem;
using AElf.GovernmentSystem;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;

namespace AElf.Blockchains.PoWChain
{
    public class PoWChainContractDeploymentListProvider : IContractDeploymentListProvider
    {
        public List<Hash> GetDeployContractNameList()
        {
            return new List<Hash>
            {
                VoteSmartContractAddressNameProvider.Name,
                ProfitSmartContractAddressNameProvider.Name,
                AssociationSmartContractAddressNameProvider.Name,
                ReferendumSmartContractAddressNameProvider.Name,
                TokenSmartContractAddressNameProvider.Name,
                ConfigurationSmartContractAddressNameProvider.Name,
                ConsensusSmartContractAddressNameProvider.Name,
                TokenConverterSmartContractAddressNameProvider.Name,
                TokenHolderSmartContractAddressNameProvider.Name,
            };
        }
    }
}