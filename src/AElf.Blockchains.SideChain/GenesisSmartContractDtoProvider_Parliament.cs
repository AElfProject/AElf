using Acs0;
using AElf.Contracts.ParliamentAuth;
using AElf.OS.Node.Application;

namespace AElf.Blockchains.SideChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateParliamentInitializationCallList()
        {
            var parliamentInitializationCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            parliamentInitializationCallList.Add(
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Initialize),
                new Contracts.ParliamentAuth.InitializeInput
                {
                    GenesisOwnerReleaseThreshold = _contractOptions.GenesisOwnerReleaseThreshold
                });
            return parliamentInitializationCallList;
        } 
    }
}