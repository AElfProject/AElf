using Acs0;
using Acs7;
using AElf.Contracts.MultiToken.Messages;
using AElf.OS.Node.Application;
using InitializeInput = AElf.Contracts.MultiToken.Messages.InitializeInput;

namespace AElf.Blockchains.SideChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateTokenInitializationCallList(ChainInitializationData chainInitializationData)
        {
            var tokenInitializationCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            tokenInitializationCallList.Add(
                nameof(TokenContractContainer.TokenContractStub.Initialize),
                new InitializeInput
                {
//                    MainChainTokenContractAddress = chainInitializationData.
                });
            return tokenInitializationCallList;
        }
    }
}