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
                ConsensusSmartContractAddressNameProvider.Name
            };
        }
    }
}