using System.Collections.Generic;
using AElf.Kernel.Consensus;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Blockchains.MainChain
{
    public class MainChainContractDeploymentListProvider : IContractDeploymentListProvider
    {
        public List<Hash> GetDeployContractNameList()
        {
            return new List<Hash>
            {
<<<<<<< HEAD
                // VoteSmartContractAddressNameProvider.Name,
                // ProfitSmartContractAddressNameProvider.Name,
                // ElectionSmartContractAddressNameProvider.Name,
                // TreasurySmartContractAddressNameProvider.Name,
                // ParliamentSmartContractAddressNameProvider.Name,
                // AssociationSmartContractAddressNameProvider.Name,
                // ReferendumSmartContractAddressNameProvider.Name,
                // TokenSmartContractAddressNameProvider.Name,
                // CrossChainSmartContractAddressNameProvider.Name,
                // ConfigurationSmartContractAddressNameProvider.Name,
                ConsensusSmartContractAddressNameProvider.Name,
                // TokenConverterSmartContractAddressNameProvider.Name,
                // TokenHolderSmartContractAddressNameProvider.Name,
                // EconomicSmartContractAddressNameProvider.Name,
=======
                ConsensusSmartContractAddressNameProvider.Name
>>>>>>> 24965b91996ac95c5055d6da940c93be37faf6dd
            };
        }
    }
}