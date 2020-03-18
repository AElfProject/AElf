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
            var nativeTokenInfo = TokenInfo.Parser.ParseFrom(chainInitializationData.NativeTokenInfoData);
            var resourceTokenList =
                TokenInfoList.Parser.ParseFrom(chainInitializationData.ResourceTokenInfo.ResourceTokenListData);
            var tokenInitializationCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();

            // native token
            tokenInitializationCallList.Add(
                nameof(TokenContractContainer.TokenContractStub.Create),
                GenerateTokenCreateInput(nativeTokenInfo));

            if (chainInitializationData.ChainPrimaryTokenInfo != null)
            {
                // primary token
                var chainPrimaryTokenInfo =
                    TokenInfo.Parser.ParseFrom(chainInitializationData.ChainPrimaryTokenInfo.ChainPrimaryTokenData);

                tokenInitializationCallList.Add(
                    nameof(TokenContractContainer.TokenContractStub.Create),
                    GenerateTokenCreateInput(chainPrimaryTokenInfo));

                tokenInitializationCallList.Add(nameof(TokenContractContainer.TokenContractStub.SetPrimaryTokenSymbol),
                    new SetPrimaryTokenSymbolInput
                    {
                        Symbol = chainPrimaryTokenInfo.Symbol
                    });

                foreach (var issueStuff in chainInitializationData.ChainPrimaryTokenInfo.SideChainTokenInitialIssueList)
                {
                    tokenInitializationCallList.Add(nameof(TokenContractContainer.TokenContractStub.Issue),
                        new IssueInput
                        {
                            Symbol = chainPrimaryTokenInfo.Symbol,
                            Amount = issueStuff.Amount,
                            Memo = "Initial issue",
                            To = issueStuff.Address
                        });
                }
            }

            // resource token
            foreach (var resourceTokenInfo in resourceTokenList.Value)
            {
                tokenInitializationCallList.Add(
                    nameof(TokenContractContainer.TokenContractStub.Create),
                    GenerateTokenCreateInput(resourceTokenInfo));
            }


            tokenInitializationCallList.Add(nameof(TokenContractContainer.TokenContractStub.Initialize),
                new InitializeInput
                {
                    ResourceAmount =
                    {
                        chainInitializationData.ResourceTokenInfo.InitialResourceAmount.ToDictionary(
                            kv => kv.Key.ToUpper(),
                            kv => kv.Value)
                    },
                    MinimumProfitsDonationPartsPerHundred =
                        chainInitializationData.MinimumProfitsDonationPartsPerHundred,
                    RegisteredOtherTokenContractAddresses =
                    {
                        [_sideChainInitializationDataProvider.ParentChainId] =
                            chainInitializationData.ParentChainTokenContractAddress
                    }
                });

            tokenInitializationCallList.Add(nameof(TokenContractContainer.TokenContractStub.SetSideChainCreator),
                chainInitializationData.Creator);

            return tokenInitializationCallList;
        }

        private CreateInput GenerateTokenCreateInput(TokenInfo tokenInfo)
        {
            return new CreateInput
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
    }
}