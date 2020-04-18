using Acs0;
using AElf.Contracts.Parliament;
using AElf.Kernel.Proposal;
using AElf.OS.Node.Application;
using AElf.Types;

namespace AElf.Blockchains.ContractInitialization
{
    public class MainChainParliamentContractInitializationProvider : ContractInitializationProviderBase
    {
        protected override Hash ContractName { get; } = ParliamentSmartContractAddressNameProvider.Name;

        protected override string ContractCodeName { get; } = "AElf.Contracts.Parliament";
        
        
        protected override SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateInitializationCallList()
        {
            var parliamentInitializationCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            parliamentInitializationCallList.Add(
                nameof(ParliamentContractContainer.ParliamentContractStub.Initialize),
                new Contracts.Parliament.InitializeInput());
            return parliamentInitializationCallList;
        }
    }
}