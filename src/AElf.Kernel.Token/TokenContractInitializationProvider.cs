using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContractInitialization;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Token
{
    public class TokenContractInitializationProvider : IContractInitializationProvider, ITransientDependency
    {
        public Hash SystemSmartContractName { get; } = TokenSmartContractAddressNameProvider.Name;
        public string ContractCodeName { get; } = "AElf.Contracts.MultiToken";

        private readonly ITokenContractInitializationDataProvider _tokenContractInitializationDataProvider;

        public TokenContractInitializationProvider(
            ITokenContractInitializationDataProvider tokenContractInitializationDataProvider)
        {
            _tokenContractInitializationDataProvider = tokenContractInitializationDataProvider;
        }

        public Dictionary<string, ByteString> GetInitializeMethodMap(byte[] contractCode)
        {
            var methodMap = new Dictionary<string, ByteString>();
            var initializationData = _tokenContractInitializationDataProvider.GetContractInitializationData();
            if (initializationData != null)
            {
                var nativeTokenInfo = TokenInfo.Parser.ParseFrom(initializationData.NativeTokenInfoData);
                var resourceTokenList =
                    TokenInfoList.Parser.ParseFrom(initializationData.ResourceTokenListData);

                // native token
                methodMap.Add(
                    nameof(TokenContractContainer.TokenContractStub.Create),
                    GenerateTokenCreateInput(nativeTokenInfo).ToByteString());

                // resource token
                foreach (var resourceTokenInfo in resourceTokenList.Value)
                {
                    methodMap.Add(
                        nameof(TokenContractContainer.TokenContractStub.Create),
                        GenerateTokenCreateInput(resourceTokenInfo).ToByteString());
                }

                methodMap.Add(nameof(TokenContractContainer.TokenContractStub.InitializeFromParentChain),
                    new InitializeFromParentChainInput
                    {
                        ResourceAmount = {initializationData.ResourceAmount},
                        RegisteredOtherTokenContractAddresses =
                        {
                            initializationData.RegisteredOtherTokenContractAddresses
                        },
                        Creator = initializationData.Creator
                    }.ToByteString());

                methodMap.Add(nameof(TokenContractContainer.TokenContractStub.InitialCoefficients),
                    new Empty().ToByteString());

                if (initializationData.PrimaryTokenInfoData != null)
                {
                    // primary token
                    var chainPrimaryTokenInfo =
                        TokenInfo.Parser.ParseFrom(initializationData.PrimaryTokenInfoData);

                    methodMap.Add(
                        nameof(TokenContractContainer.TokenContractStub.Create),
                        GenerateTokenCreateInput(chainPrimaryTokenInfo).ToByteString());

                    foreach (var issueStuff in initializationData.TokenInitialIssueList)
                    {
                        methodMap.Add(nameof(TokenContractContainer.TokenContractStub.Issue),
                            new IssueInput
                            {
                                Symbol = chainPrimaryTokenInfo.Symbol,
                                Amount = issueStuff.Amount,
                                Memo = "Initial issue",
                                To = issueStuff.Address
                            }.ToByteString());
                    }

                    methodMap.Add(nameof(TokenContractContainer.TokenContractStub.SetPrimaryTokenSymbol),
                        new SetPrimaryTokenSymbolInput
                        {
                            Symbol = chainPrimaryTokenInfo.Symbol
                        }.ToByteString());
                }
                else
                {
                    // set primary token with native token 
                    methodMap.Add(nameof(TokenContractContainer.TokenContractStub.SetPrimaryTokenSymbol),
                        new SetPrimaryTokenSymbolInput
                        {
                            Symbol = nativeTokenInfo.Symbol
                        }.ToByteString());
                }
            }

            return methodMap;
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