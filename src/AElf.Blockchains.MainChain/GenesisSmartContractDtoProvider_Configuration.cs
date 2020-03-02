using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.Configuration;
using AElf.Kernel;
using AElf.OS.Node.Application;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        private IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForConfiguration()
        {
            var l = new List<GenesisSmartContractDto>();
            l.AddGenesisSmartContract(
                GetContractCodeByName("AElf.Contracts.Configuration"),
                ConfigurationSmartContractAddressNameProvider.Name, GenerateConfigurationInitializationCallList());
            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateConfigurationInitializationCallList()
        {
            var configurationContractMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            configurationContractMethodCallList.Add(
                nameof(ConfigurationContainer.ConfigurationStub.SetRequiredAcsInContracts),
                new RequiredAcsInContracts
                {
                    AcsList = {_contractOptions.ContractFeeStrategyAcsList}
                });
            return configurationContractMethodCallList;
        }
    }
}
