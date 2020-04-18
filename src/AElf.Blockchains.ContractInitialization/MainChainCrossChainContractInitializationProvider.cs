using Acs0;
using AElf.Contracts.CrossChain;
using AElf.CrossChain;
using AElf.OS.Node.Application;
using AElf.Types;

namespace AElf.Blockchains.ContractInitialization
{
    public class MainChainCrossChainContractInitializationProvider : ContractInitializationProviderBase
    {
        protected override Hash ContractName { get; } = CrossChainSmartContractAddressNameProvider.Name;

        protected override string ContractCodeName { get; } = "AElf.Contracts.CrossChain";
        
        
        protected override SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateInitializationCallList()
        {
            var crossChainMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            crossChainMethodCallList.Add(nameof(CrossChainContractContainer.CrossChainContractStub.Initialize),
                new InitializeInput
                {
                    IsPrivilegePreserved = true
                });
            return crossChainMethodCallList;
        }
    }
}