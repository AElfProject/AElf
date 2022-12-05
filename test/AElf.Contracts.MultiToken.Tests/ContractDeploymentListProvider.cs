using System.Collections.Generic;
using AElf.EconomicSystem;
using AElf.GovernmentSystem;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;

namespace AElf.Contracts.MultiToken;

public class ContractDeploymentListProvider : IContractDeploymentListProvider
{
    public List<Hash> GetDeployContractNameList()
    {
        return new List<Hash>
        {
            TokenSmartContractAddressNameProvider.Name,
            ProfitSmartContractAddressNameProvider.Name,
            TreasurySmartContractAddressNameProvider.Name,
            TokenConverterSmartContractAddressNameProvider.Name,
            ReferendumSmartContractAddressNameProvider.Name,
            HashHelper.ComputeFrom("AElf.TestContractNames.BasicFunction"),
            HashHelper.ComputeFrom("AElf.TestContractNames.OtherBasicFunction"),
            ParliamentSmartContractAddressNameProvider.Name,
            ConsensusSmartContractAddressNameProvider.Name,
            AssociationSmartContractAddressNameProvider.Name,
            HashHelper.ComputeFrom("AElf.ContractNames.NFT"),
        };
    }
}