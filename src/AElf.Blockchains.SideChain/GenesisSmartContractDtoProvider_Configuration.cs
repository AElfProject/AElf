using Acs0;
using Acs7;
using AElf.Contracts.Configuration;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS.Node.Application;
using Google.Protobuf;

namespace AElf.Blockchains.SideChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        public SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateConfigurationInitializationCallList(ChainInitializationData chainInitializationData)
        {
            var configurationContractMethodCallList =
                new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            return configurationContractMethodCallList;
        }
    }
}