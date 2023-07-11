using System.Collections.Generic;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken;

public class SideChainUnitTestTokenContractInitializationProvider : TokenContractInitializationProvider
{
    private readonly ITokenContractInitializationDataProvider _tokenContractInitializationDataProvider;

    public SideChainUnitTestTokenContractInitializationProvider(
        ITokenContractInitializationDataProvider tokenContractInitializationDataProvider) : base(
        tokenContractInitializationDataProvider)
    {
        _tokenContractInitializationDataProvider = tokenContractInitializationDataProvider;
    }

    public override List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
    {
        var methodList = new List<ContractInitializationMethodCall>();
        var initializationData = _tokenContractInitializationDataProvider.GetContractInitializationData();
        var nativeTokenInfo = TokenInfo.Parser.ParseFrom(initializationData.NativeTokenInfoData);
        var resourceTokenList =
            TokenInfoList.Parser.ParseFrom(initializationData.ResourceTokenListData);

        // native token
        methodList.Add(new ContractInitializationMethodCall
        {
            MethodName = nameof(TokenContractImplContainer.TokenContractImplStub.Create),
            Params = GenerateTokenCreateInput(nativeTokenInfo).ToByteString()
        });

        // resource token
        foreach (var resourceTokenInfo in resourceTokenList.Value)
            methodList.Add(new ContractInitializationMethodCall
            {
                MethodName = nameof(TokenContractImplContainer.TokenContractImplStub.Create),
                Params = GenerateTokenCreateInput(resourceTokenInfo).ToByteString()
            });

        methodList.Add(new ContractInitializationMethodCall
        {
            MethodName = nameof(TokenContractImplContainer.TokenContractImplStub.InitialCoefficients),
            Params = new Empty().ToByteString()
        });

        if (initializationData.PrimaryTokenInfoData != null)
        {
            // primary token
            var chainPrimaryTokenInfo =
                TokenInfo.Parser.ParseFrom(initializationData.PrimaryTokenInfoData);

            methodList.Add(new ContractInitializationMethodCall
            {
                MethodName = nameof(TokenContractImplContainer.TokenContractImplStub.Create),
                Params = GenerateTokenCreateInput(chainPrimaryTokenInfo).ToByteString()
            });

            foreach (var issueStuff in initializationData.TokenInitialIssueList)
                methodList.Add(new ContractInitializationMethodCall
                {
                    MethodName = nameof(TokenContractImplContainer.TokenContractImplStub.Issue),
                    Params = new IssueInput
                    {
                        Symbol = chainPrimaryTokenInfo.Symbol,
                        Amount = issueStuff.Amount,
                        Memo = "Initial issue",
                        To = issueStuff.Address
                    }.ToByteString()
                });

            methodList.Add(new ContractInitializationMethodCall
            {
                MethodName = nameof(TokenContractImplContainer.TokenContractImplStub.SetPrimaryTokenSymbol),
                Params = new SetPrimaryTokenSymbolInput
                {
                    Symbol = chainPrimaryTokenInfo.Symbol
                }.ToByteString()
            });
        }
        else
        {
            // set primary token with native token 
            methodList.Add(new ContractInitializationMethodCall
            {
                MethodName = nameof(TokenContractImplContainer.TokenContractImplStub.SetPrimaryTokenSymbol),
                Params = new SetPrimaryTokenSymbolInput
                {
                    Symbol = nativeTokenInfo.Symbol
                }.ToByteString()
            });
        }

        // if (initializationData.RegisteredOtherTokenContractAddresses.Values.All(v=>v != null))
        // {
        //     methodList.Add(new ContractInitializationMethodCall
        //     {
        //         MethodName = nameof(TokenContractImplContainer.TokenContractImplStub.InitializeFromParentChain),
        //         Params = new InitializeFromParentChainInput
        //         {
        //             ResourceAmount = {initializationData.ResourceAmount},
        //             RegisteredOtherTokenContractAddresses =
        //             {
        //                 initializationData.RegisteredOtherTokenContractAddresses
        //             },
        //             Creator = initializationData.Creator
        //         }.ToByteString()
        //     });
        // }

        methodList.Add(new ContractInitializationMethodCall
        {
            MethodName = nameof(TokenContractImplContainer.TokenContractImplStub.InitializeAuthorizedController),
            Params = ByteString.Empty
        });


        return methodList;
    }

    private CreateInput GenerateTokenCreateInput(TokenInfo tokenInfo)
    {
        return new CreateInput
        {
            Decimals = tokenInfo.Decimals,
            IssueChainId = tokenInfo.IssueChainId,
            Issuer = tokenInfo.Issuer,
            Owner = tokenInfo.Issuer,
            IsBurnable = tokenInfo.IsBurnable,
            Symbol = tokenInfo.Symbol,
            TokenName = tokenInfo.TokenName,
            TotalSupply = tokenInfo.TotalSupply
        };
    }
}