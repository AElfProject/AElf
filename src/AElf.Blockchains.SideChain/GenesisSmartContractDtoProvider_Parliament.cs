using Acs0;
using Acs7;
using AElf.Contracts.ParliamentAuth;
using AElf.OS.Node.Application;

namespace AElf.Blockchains.SideChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateParliamentInitializationCallList(ChainInitializationData chainInitializationData)
        {
            var parliamentInitializationCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            parliamentInitializationCallList.Add(
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Initialize),
                new Contracts.ParliamentAuth.InitializeInput
                {
                    GenesisOwnerReleaseThreshold = _contractOptions.GenesisOwnerReleaseThreshold,
                    PrivilegedProposer = chainInitializationData.Creator,
                    ProposerAuthorityRequired = chainInitializationData.ChainCreatorPrivilegePreserved
                });
            return parliamentInitializationCallList;
        } 
    }
}