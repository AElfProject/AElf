using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.Configuration;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.TestBase;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.Configuration;
using AElf.Kernel.Proposal;
using AElf.Kernel.Token;
using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;

namespace AElf.Contracts.ConfigurationContract.Tests;

public class ConfigurationContractTestBase : ContractTestBase<ConfigurationContractTestAElfModule>
{
    protected long _balanceOfStarter;

    protected long _totalSupply;
    protected Address ConfigurationContractAddress;
    protected Address ParliamentAddress;
    protected Address TokenContractAddress;

    public ConfigurationContractTestBase()
    {
        AsyncHelper.RunSync(() =>
            Tester.InitialChainAsync(Tester.GetDefaultContractTypes(Tester.GetCallOwnerAddress(), out _totalSupply,
                out _, out _balanceOfStarter, true)));
        ParliamentAddress = Tester.GetContractAddress(ParliamentSmartContractAddressNameProvider.Name);
        ConfigurationContractAddress =
            Tester.GetContractAddress(ConfigurationSmartContractAddressNameProvider.Name);
        TokenContractAddress = Tester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
    }

    protected async Task<TransactionResult> ExecuteContractWithMiningAsync(Address contractAddress,
        string methodName, IMessage input)
    {
        return await Tester.ExecuteContractWithMiningAsync(contractAddress, methodName, input);
    }

    protected async Task<Transaction> GenerateTransactionAsync(Address contractAddress, string methodName,
        ECKeyPair ecKeyPair, IMessage input)
    {
        return ecKeyPair == null
            ? await Tester.GenerateTransactionAsync(contractAddress, methodName, input)
            : await Tester.GenerateTransactionAsync(contractAddress, methodName, ecKeyPair, input);
    }

    internal SetConfigurationInput SetBlockTransactionLimitRequest(int amount)
    {
        return new SetConfigurationInput
        {
            Key = "BlockTransactionLimit",
            Value = new Int32Value { Value = amount }.ToByteString()
        };
    }

    internal async Task<Hash> SetBlockTransactionLimitProposalAsync(int amount)
    {
        var createProposalInput = SetBlockTransactionLimitRequest(amount);
        var organizationAddress = await GetParliamentDefaultOrganizationAddressAsync();
        var proposalId =
            await CreateProposalAsync(organizationAddress, createProposalInput, "SetConfiguration");
        return proposalId;
    }

    internal async Task<Address> GetParliamentDefaultOrganizationAddressAsync()
    {
        var organizationAddress = Address.Parser.ParseFrom((await Tester.ExecuteContractWithMiningAsync(
                ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.GetDefaultOrganizationAddress),
                new Empty()))
            .ReturnValue);
        return organizationAddress;
    }

    internal async Task<Hash> CreateProposalAsync(Address organizationAddress, IMessage input, string methodName)
    {
        var proposal = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
            nameof(AuthorizationContractContainer.AuthorizationContractStub.CreateProposal),
            new CreateProposalInput
            {
                ContractMethodName = methodName,
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                Params = input.ToByteString(),
                ToAddress = ConfigurationContractAddress,
                OrganizationAddress = organizationAddress
            });
        var proposalId = Hash.Parser.ParseFrom(proposal.ReturnValue);
        return proposalId;
    }

    protected async Task ApproveWithMinersAsync(Hash proposalId)
    {
        var approveTransaction1 = await GenerateTransactionAsync(ParliamentAddress,
            nameof(AuthorizationContractContainer.AuthorizationContractStub.Approve), Tester.InitialMinerList[0],
            proposalId);
        var approveTransaction2 = await GenerateTransactionAsync(ParliamentAddress,
            nameof(AuthorizationContractContainer.AuthorizationContractStub.Approve), Tester.InitialMinerList[1],
            proposalId);
        var approveTransaction3 = await GenerateTransactionAsync(ParliamentAddress,
            nameof(AuthorizationContractContainer.AuthorizationContractStub.Approve), Tester.InitialMinerList[2],
            proposalId);

        // Mine a block with given normal txs and system txs.
        await Tester.MineAsync(
            new List<Transaction> { approveTransaction1, approveTransaction2, approveTransaction3 });
    }

    protected async Task<TransactionResult> ReleaseProposalAsync(Hash proposalId)
    {
        var transactionResult = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
            nameof(AuthorizationContractContainer.AuthorizationContractStub.Release), proposalId);
        return transactionResult;
    }

    internal async Task<Hash> SetTransactionOwnerAddressProposalAsync(AuthorityInfo authorityInfo)
    {
        var organizationAddress = Address.Parser.ParseFrom((await Tester.ExecuteContractWithMiningAsync(
                ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.GetDefaultOrganizationAddress),
                new Empty()))
            .ReturnValue);
        var proposal = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
            nameof(AuthorizationContractContainer.AuthorizationContractStub.CreateProposal),
            new CreateProposalInput
            {
                ContractMethodName =
                    nameof(ConfigurationImplContainer.ConfigurationImplStub.ChangeConfigurationController),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                Params = authorityInfo.ToByteString(),
                ToAddress = ConfigurationContractAddress,
                OrganizationAddress = organizationAddress
            });
        var proposalId = Hash.Parser.ParseFrom(proposal.ReturnValue);
        return proposalId;
    }

    protected async Task<Hash> CreateProposalAsync(ContractTester<ConfigurationContractTestAElfModule> tester,
        Address contractAddress, Address organizationAddress, string methodName, IMessage input)
    {
        var configContract = tester.GetContractAddress(HashHelper.ComputeFrom("AElf.ContractNames.Configuration"));
        var proposal = await tester.ExecuteContractWithMiningAsync(contractAddress,
            nameof(AuthorizationContractContainer.AuthorizationContractStub.CreateProposal),
            new CreateProposalInput
            {
                ContractMethodName = methodName,
                ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                Params = input.ToByteString(),
                ToAddress = configContract,
                OrganizationAddress = organizationAddress
            });
        var proposalId = Hash.Parser.ParseFrom(proposal.ReturnValue);
        return proposalId;
    }

    protected async Task<AuthorityInfo> GetMethodFeeController(Address configurationContractAddress)
    {
        var methodFeeControllerByteString = await Tester.CallContractMethodAsync(configurationContractAddress,
            nameof(MethodFeeProviderContractContainer.MethodFeeProviderContractStub.GetMethodFeeController),
            new Empty());
        return AuthorityInfo.Parser.ParseFrom(methodFeeControllerByteString);
    }

    protected async Task CreateTokenAsync(string symbol)
    {
        await Tester.ExecuteContractWithMiningAsync(TokenContractAddress,
            nameof(TokenContractContainer.TokenContractStub.Create),
            new CreateInput
            {
                Symbol = "SEED-0",
                Decimals = 0,
                IsBurnable = true,
                TokenName = "seed Collection",
                TotalSupply = 1,
                Issuer = Tester.GetCallOwnerAddress(),
                ExternalInfo = new ExternalInfo(),
                Owner = Tester.GetCallOwnerAddress()
            });
        await Tester.ExecuteContractWithMiningAsync(TokenContractAddress,
            nameof(TokenContractContainer.TokenContractStub.Create),
            new CreateInput
            {
                Symbol = "SEED-1",
                Decimals = 0,
                IsBurnable = true,
                TokenName = "seed",
                TotalSupply = 1,
                Issuer = Tester.GetCallOwnerAddress(),
                ExternalInfo = new ExternalInfo
                {
                    Value =
                    {
                        { "__seed_owned_symbol", symbol },
                        { "__seed_exp_time", TimestampHelper.GetUtcNow().AddDays(1).Seconds.ToString() }
                    }
                },
                LockWhiteList = { TokenContractAddress },
                Owner = Tester.GetCallOwnerAddress()
            });

        await Tester.ExecuteContractWithMiningAsync(TokenContractAddress,
            nameof(TokenContractContainer.TokenContractStub.Issue),
            new IssueInput
            {
                Symbol = "SEED-1",
                Amount = 1,
                To = Tester.GetCallOwnerAddress(),
                Memo = "test"
            });
    }
}