using System.Collections.Generic;
using AElf.GovernmentSystem;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;

namespace AElf.Contracts.AEDPoS
{
    public class ContractDeploymentListProvider : IContractDeploymentListProvider
    {
        public List<Hash> GetDeployContractNameList()
        {
            return new List<Hash>
            {
                VoteSmartContractAddressNameProvider.Name,
                ElectionSmartContractAddressNameProvider.Name,
                TokenSmartContractAddressNameProvider.Name,
                ConsensusSmartContractAddressNameProvider.Name,
                ElectionSmartContractAddressNameProvider.Name,
            };
        }
    }
}