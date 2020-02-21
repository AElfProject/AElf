using System.Linq;
using Acs0;
using Acs7;
using AElf.Contracts.MultiToken;
using AElf.OS.Node.Application;
using InitializeInput = AElf.Contracts.MultiToken.InitializeInput;

namespace AElf.Blockchains.SideChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateTokenInitializationCallList(
            ChainInitializationData chainInitializationData)
        {
            var nativeTokenInfo = TokenInfo.Parser.ParseFrom(chainInitializationData.ExtraInformation[1]);
            var resourceTokenList = TokenInfoList.Parser.ParseFrom(chainInitializationData.ExtraInformation[2]);
            var chainPrimaryTokenInfo = TokenInfo.Parser.ParseFrom(chainInitializationData.ExtraInformation[3]);
            var tokenInitializationCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            tokenInitializationCallList.Add(
                nameof(TokenContractContainer.TokenContractStub.RegisterNativeAndResourceTokenInfo),
                new RegisterNativeAndResourceTokenInfoInput
                {
                    NativeTokenInfo =
                        new RegisterNativeTokenInfoInput
                        {
                            Decimals = nativeTokenInfo.Decimals,
                            IssueChainId = nativeTokenInfo.IssueChainId,
                            Issuer = nativeTokenInfo.Issuer,
                            IsBurnable = nativeTokenInfo.IsBurnable,
                            Symbol = nativeTokenInfo.Symbol,
                            TokenName = nativeTokenInfo.TokenName,
                            TotalSupply = nativeTokenInfo.TotalSupply,
                            IsProfitable = nativeTokenInfo.IsProfitable
                        },
                    ResourceTokenList = GenerateInitialResourceTokenInfoList(resourceTokenList),
                    ChainPrimaryToken = GenerateInitialChainPrimaryTokenInfo(chainPrimaryTokenInfo)
                });

            foreach (var issueStuff in chainInitializationData.SideChainTokenInitialIssueList)
            {
                tokenInitializationCallList.Add(nameof(TokenContractContainer.TokenContractStub.Issue), new IssueInput
                {
                    Symbol = chainPrimaryTokenInfo.Symbol,
                    Amount = issueStuff.Amount,
                    Memo = "Initial issue",
                    To = issueStuff.Address
                });
            }

            tokenInitializationCallList.Add(nameof(TokenContractContainer.TokenContractStub.Initialize),
                new InitializeInput
                {
                    ResourceAmount =
                    {
                        chainInitializationData.InitialResourceAmount.ToDictionary(kv => kv.Key.ToUpper(),
                            kv => kv.Value)
                    }
                });

            tokenInitializationCallList.Add(nameof(TokenContractContainer.TokenContractStub.SetSideChainCreator),
                chainInitializationData.Creator);

            return tokenInitializationCallList;
        }

        private TokenInfo GenerateInitialChainPrimaryTokenInfo(TokenInfo tokenInfo)
        {
            return new TokenInfo
            {
                Decimals = tokenInfo.Decimals,
                IssueChainId = tokenInfo.IssueChainId,
                Issuer = tokenInfo.Issuer,
                IsBurnable = tokenInfo.IsBurnable,
                Symbol = tokenInfo.Symbol,
                TokenName = tokenInfo.TokenName,
                TotalSupply = tokenInfo.TotalSupply,
                IsProfitable = tokenInfo.IsProfitable
            };
        }

        private TokenInfoList GenerateInitialResourceTokenInfoList(TokenInfoList tokenInfoList)
        {
            var resourceTokenInfoList = new TokenInfoList();
            foreach (var resourceToken in tokenInfoList.Value)
            {
                // make sure it is consistent with old data 
                resourceTokenInfoList.Value.Add(new TokenInfo
                {
                    Decimals = resourceToken.Decimals,
                    IssueChainId = resourceToken.IssueChainId,
                    Issuer = resourceToken.Issuer,
                    IsBurnable = resourceToken.IsBurnable,
                    Symbol = resourceToken.Symbol,
                    TokenName = resourceToken.TokenName,
                    Supply = resourceToken.TotalSupply,
                    TotalSupply = resourceToken.TotalSupply,
                    IsProfitable = resourceToken.IsProfitable
                });
            }

            return resourceTokenInfoList;
        }
    }
}