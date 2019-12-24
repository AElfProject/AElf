using Acs0;
using Acs7;
using AElf.Contracts.Parliament;
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
                nameof(ParliamentContractContainer.ParliamentContractStub.Initialize),
                new Contracts.Parliament.InitializeInput
                {
                    PrivilegedProposer = chainInitializationData.Creator,
                    ProposerAuthorityRequired = chainInitializationData.ChainCreatorPrivilegePreserved
                });
            return parliamentInitializationCallList;
        } 
    }
}