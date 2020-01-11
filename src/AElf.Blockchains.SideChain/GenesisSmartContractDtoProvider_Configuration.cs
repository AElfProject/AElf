using Acs0;
using Acs7;
using AElf.Contracts.Configuration;
using AElf.OS.Node.Application;

namespace AElf.Blockchains.SideChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        public SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateConfigurationInitializationCallList(ChainInitializationData chainInitializationData)
        {
            var crossChainMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            var requiredAcsInContracts = new RequiredAcsInContracts();
            if (!chainInitializationData.ChainCreatorPrivilegePreserved)
                requiredAcsInContracts.AcsList.AddRange(_contractOptions.ContractFeeStrategyAcsList);
            crossChainMethodCallList.Add(nameof(ConfigurationContainer.ConfigurationStub.SetRequiredAcsInContracts),
                requiredAcsInContracts);
            return crossChainMethodCallList;
        }
    }
}