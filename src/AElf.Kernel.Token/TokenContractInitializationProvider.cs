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

        public List<InitializeMethod> GetInitializeMethodList(byte[] contractCode)
        {
            var methodList = new List<InitializeMethod>();
            var initializationData = _tokenContractInitializationDataProvider.GetContractInitializationData();
            
            // For the main chain, we use the economic contract to initialize the token contract.
            // So no initialization methods are required in here.
            // But for the side chain, which has no economic contract, we need initialize token contract.
            if (initializationData != null)
            {
                var nativeTokenInfo = TokenInfo.Parser.ParseFrom(initializationData.NativeTokenInfoData);
                var resourceTokenList =
                    TokenInfoList.Parser.ParseFrom(initializationData.ResourceTokenListData);

                // native token
                methodList.Add(new InitializeMethod
                {
                    MethodName = nameof(TokenContractContainer.TokenContractStub.Create),
                    Params = GenerateTokenCreateInput(nativeTokenInfo).ToByteString()
                });

                // resource token
                foreach (var resourceTokenInfo in resourceTokenList.Value)
                {
                    methodList.Add(new InitializeMethod
                    {
                        MethodName = nameof(TokenContractContainer.TokenContractStub.Create),
                        Params = GenerateTokenCreateInput(resourceTokenInfo).ToByteString()
                    });
                }

                methodList.Add(new InitializeMethod
                {
                    MethodName = nameof(TokenContractContainer.TokenContractStub.InitializeFromParentChain),
                    Params = new InitializeFromParentChainInput
                    {
                        ResourceAmount = {initializationData.ResourceAmount},
                        RegisteredOtherTokenContractAddresses =
                        {
                            initializationData.RegisteredOtherTokenContractAddresses
                        },
                        Creator = initializationData.Creator
                    }.ToByteString()
                });

                methodList.Add(new InitializeMethod
                {
                    MethodName = nameof(TokenContractContainer.TokenContractStub.InitialCoefficients),
                    Params = new Empty().ToByteString()
                });

                if (initializationData.PrimaryTokenInfoData != null)
                {
                    // primary token
                    var chainPrimaryTokenInfo =
                        TokenInfo.Parser.ParseFrom(initializationData.PrimaryTokenInfoData);

                    methodList.Add(new InitializeMethod
                    {
                        MethodName = nameof(TokenContractContainer.TokenContractStub.Create),
                        Params = GenerateTokenCreateInput(chainPrimaryTokenInfo).ToByteString()
                    });

                    foreach (var issueStuff in initializationData.TokenInitialIssueList)
                    {
                        methodList.Add(new InitializeMethod
                        {
                            MethodName = nameof(TokenContractContainer.TokenContractStub.Issue),
                            Params = new IssueInput
                            {
                                Symbol = chainPrimaryTokenInfo.Symbol,
                                Amount = issueStuff.Amount,
                                Memo = "Initial issue",
                                To = issueStuff.Address
                            }.ToByteString()
                        });
                    }

                    methodList.Add(new InitializeMethod
                    {
                        MethodName = nameof(TokenContractContainer.TokenContractStub.SetPrimaryTokenSymbol),
                        Params = new SetPrimaryTokenSymbolInput
                        {
                            Symbol = chainPrimaryTokenInfo.Symbol
                        }.ToByteString()
                    });
                }
                else
                {
                    // set primary token with native token 
                    methodList.Add(new InitializeMethod
                    {
                        MethodName = nameof(TokenContractContainer.TokenContractStub.SetPrimaryTokenSymbol),
                        Params = new SetPrimaryTokenSymbolInput
                        {
                            Symbol = nativeTokenInfo.Symbol
                        }.ToByteString()
                    });
                }
            }

            return methodList;
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