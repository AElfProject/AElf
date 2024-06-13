using System.Collections.Generic;
using AElf.Kernel.Configuration;
using AElf.Kernel.Consensus;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;

namespace AElf.Blockchains.MainChain;

public class MainChainContractDeploymentListProvider : IContractDeploymentListProvider
{
    public List<Hash> GetDeployContractNameList()
    {
        return
        [
            TokenSmartContractAddressNameProvider.Name,
            ConfigurationSmartContractAddressNameProvider.Name,
            ConsensusSmartContractAddressNameProvider.Name
        ];
    }
}