using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
using AElf.Kernel.SmartContractInitialization;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests
{
    public class MethodFeeTestTokenContractInitializationProvider : TokenContractInitializationProvider,
        IContractInitializationProvider
    {
        public MethodFeeTestTokenContractInitializationProvider(
            ITokenContractInitializationDataProvider tokenContractInitializationDataProvider) : base(
            tokenContractInitializationDataProvider)
        {
        }

        public new List<InitializeMethod> GetInitializeMethodList(byte[] contractCode)
        {
            var methodList = new List<InitializeMethod>();
            // native token
            methodList.Add(new InitializeMethod
            {
                MethodName = nameof(TokenContractContainer.TokenContractStub.Create),
                Params = new CreateInput
                {
                    Symbol = "ELF",
                    Decimals = 2,
                    IsBurnable = true,
                    TokenName = "elf token",
                    TotalSupply = 1_000_000_00000000L,
                    Issuer = SampleAccount.Accounts[0].Address
                }.ToByteString()
            });

            methodList.Add(new InitializeMethod
            {
                MethodName = nameof(TokenContractContainer.TokenContractStub.Issue),
                Params = new IssueInput
                {
                    Symbol = "ELF",
                    Amount = 1_000_000_00000000L,
                    To = SampleAccount.Accounts[0].Address,
                    Memo = "Set for token converter."
                }.ToByteString()
            });

            return methodList;
        }
    }
}