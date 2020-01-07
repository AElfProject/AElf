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
            crossChainMethodCallList.Add(
                nameof(ConfigurationContainer.ConfigurationStub.SetContractFeeChargingPolicy),
                new SetContractFeeChargingPolicyInput
                {
                    ContractFeeChargingPolicy = chainInitializationData.ChainCreatorPrivilegePreserved
                        ? ContractFeeChargingPolicy.NoneContractFee
                        : ContractFeeChargingPolicy.AnyContractFeeType
                });
            return crossChainMethodCallList;
        }
    }
}