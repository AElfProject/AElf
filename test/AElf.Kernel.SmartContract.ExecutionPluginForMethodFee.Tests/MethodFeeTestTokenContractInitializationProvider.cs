using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.ContractTestKit;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests;

public class MethodFeeTestTokenContractInitializationProvider : TokenContractInitializationProvider,
    IContractInitializationProvider
{
    public MethodFeeTestTokenContractInitializationProvider() : base(null)
    {
    }

    public new List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
    {
        var methodList = new List<ContractInitializationMethodCall>();
        // native token
        methodList.Add(new ContractInitializationMethodCall
        {
            MethodName = nameof(TokenContractContainer.TokenContractStub.Create),
            Params = new CreateInput
            {
                Symbol = "ELF",
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = 1_000_000_00000000L,
                Issuer = SampleAccount.Accounts[0].Address,
                Owner = SampleAccount.Accounts[0].Address
            }.ToByteString()
        });

        methodList.Add(new ContractInitializationMethodCall
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