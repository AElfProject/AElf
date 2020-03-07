using Acs0;
using Acs7;
using AElf.Contracts.Configuration;
using AElf.Kernel.Configuration;
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
            var requiredAcsInContracts = new RequiredAcsInContracts();
            if (!chainInitializationData.ChainCreatorPrivilegePreserved)
                requiredAcsInContracts.AcsList.AddRange(_contractOptions.ContractFeeStrategyAcsList);
            configurationContractMethodCallList.Add(nameof(ConfigurationContainer.ConfigurationStub.SetConfiguration),
                new SetConfigurationInput
                {
                    Key = RequiredAcsInContractsConfigurationNameProvider.Name,
                    Value = requiredAcsInContracts.ToByteString()
                });
            return configurationContractMethodCallList;
        }
    }
}