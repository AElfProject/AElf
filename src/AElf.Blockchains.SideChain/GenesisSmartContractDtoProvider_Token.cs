using Acs0;
using Acs7;
using AElf.Contracts.MultiToken.Messages;
using AElf.OS.Node.Application;

namespace AElf.Blockchains.SideChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateTokenInitializationCallList(ChainInitializationData chainInitializationData)
        {
            var nativeTokenInfo = TokenInfo.Parser.ParseFrom(chainInitializationData.ExtraInformation[1]);
            var tokenInitializationCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            tokenInitializationCallList.Add(
                nameof(TokenContractContainer.TokenContractStub.RegisterNativeTokenInfo),
                new RegisterNativeTokenInfoInput
                {
                    Decimals = nativeTokenInfo.Decimals,
                    IssueChainId = nativeTokenInfo.IssueChainId,
                    Issuer = nativeTokenInfo.Issuer,
                    IsBurnable = nativeTokenInfo.IsBurnable,
                    Symbol = nativeTokenInfo.Symbol,
                    TokenName = nativeTokenInfo.TokenName,
                    TotalSupply = nativeTokenInfo.TotalSupply
                });
                
            return tokenInitializationCallList;
        }
    }
}