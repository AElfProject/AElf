using System.Linq;
using Acs0;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;

namespace AElf.Blockchains.ContractInitialization
{
    public class SideChainTokenContractInitializationProvider : ContractInitializationProviderBase
    {
        protected override Hash ContractName { get; } = TokenSmartContractAddressNameProvider.Name;

        protected override string ContractCodeName { get; } = "AElf.Contracts.MultiToken";

        private readonly ISideChainInitializationDataProvider _sideChainInitializationDataProvider;

        public SideChainTokenContractInitializationProvider(
            ISideChainInitializationDataProvider sideChainInitializationDataProvider)
        {
            _sideChainInitializationDataProvider = sideChainInitializationDataProvider;
        }
        
        protected override SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateInitializationCallList()
        {
            var chainInitializationData = AsyncHelper.RunSync(async () =>
                await _sideChainInitializationDataProvider.GetChainInitializationDataAsync());
            
            var nativeTokenInfo = TokenInfo.Parser.ParseFrom(chainInitializationData.NativeTokenInfoData);
            var resourceTokenList =
                TokenInfoList.Parser.ParseFrom(chainInitializationData.ResourceTokenInfo.ResourceTokenListData);
            var tokenInitializationCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();

            // native token
            tokenInitializationCallList.Add(
                nameof(TokenContractContainer.TokenContractStub.Create),
                GenerateTokenCreateInput(nativeTokenInfo));

            // resource token
            foreach (var resourceTokenInfo in resourceTokenList.Value)
            {
                tokenInitializationCallList.Add(
                    nameof(TokenContractContainer.TokenContractStub.Create),
                    GenerateTokenCreateInput(resourceTokenInfo));
            }
            
            tokenInitializationCallList.Add(nameof(TokenContractContainer.TokenContractStub.InitializeFromParentChain),
                new InitializeFromParentChainInput
                {
                    ResourceAmount =
                    {
                        chainInitializationData.ResourceTokenInfo.InitialResourceAmount.ToDictionary(
                            kv => kv.Key.ToUpper(),
                            kv => kv.Value)
                    },
                    RegisteredOtherTokenContractAddresses =
                    {
                        [_sideChainInitializationDataProvider.ParentChainId] =
                            chainInitializationData.ParentChainTokenContractAddress
                    },
                    Creator = chainInitializationData.Creator
                });

            tokenInitializationCallList.Add(nameof(TokenContractContainer.TokenContractStub.InitialCoefficients),
                new Empty());
            
            if (chainInitializationData.ChainPrimaryTokenInfo != null)
            {
                // primary token
                var chainPrimaryTokenInfo =
                    TokenInfo.Parser.ParseFrom(chainInitializationData.ChainPrimaryTokenInfo.ChainPrimaryTokenData);

                tokenInitializationCallList.Add(
                    nameof(TokenContractContainer.TokenContractStub.Create),
                    GenerateTokenCreateInput(chainPrimaryTokenInfo));

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
                
                tokenInitializationCallList.Add(nameof(TokenContractContainer.TokenContractStub.SetPrimaryTokenSymbol),
                    new SetPrimaryTokenSymbolInput
                    {
                        Symbol = chainPrimaryTokenInfo.Symbol
                    });
            }
            else
            {
                // set primary token with native token 
                tokenInitializationCallList.Add(nameof(TokenContractContainer.TokenContractStub.SetPrimaryTokenSymbol),
                    new SetPrimaryTokenSymbolInput
                    {
                        Symbol = nativeTokenInfo.Symbol
                    });
            }

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