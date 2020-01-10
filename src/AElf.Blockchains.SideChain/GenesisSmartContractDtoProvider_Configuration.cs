using System.Linq;
using Acs0;
using Acs7;
using AElf.Contracts.Configuration;
using AElf.Kernel.SmartContract;
using AElf.OS.Node.Application;

namespace AElf.Blockchains.SideChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        public SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateConfigurationInitializationCallList(ChainInitializationData chainInitializationData)
        {
            var crossChainMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            crossChainMethodCallList.Add(
                nameof(ConfigurationContainer.ConfigurationStub.SetRequiredAcsInContracts),
                new RequiredAcsInContracts()
                {
                    AcsList = chainInitializationData.ChainCreatorPrivilegePreserved ? SmartContractConstants.ContractFeeStrategyAcsList : ""
                });
            return crossChainMethodCallList;
        }
    }
}