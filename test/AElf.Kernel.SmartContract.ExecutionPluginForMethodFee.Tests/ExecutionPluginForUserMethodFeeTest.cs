using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Configuration;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.FeeCalculation.Extensions;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForUserFee.Tests.TestContract;
using AElf.Kernel.Token;
using AElf.Standards.ACS12;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests;

public class ExecutionPluginForUserMethodFeeTest : ExecutionPluginForUserMethodFeeTestBase
{
    private Address _testContractAddress;
    private TestContractContainer.TestContractStub _testContractStub;
    private readonly IBlockchainService _blockchainService;
    private readonly ITotalTransactionFeesMapProvider _totalTransactionFeesMapProvider;
    private const string ConfigurationKey = "User MethodFee";

    public ExecutionPluginForUserMethodFeeTest()
    {
        _blockchainService = GetRequiredService<IBlockchainService>();
        _totalTransactionFeesMapProvider = GetRequiredService<ITotalTransactionFeesMapProvider>();
    }

    [Fact]
    public async Task GetPreTransactionFeeTest()
    {
        await Initialize();
        {
            var plugin = GetCreateInstance<IPreExecutionPlugin, UserFeeChargePreExecutionPlugin>();
            plugin.ShouldNotBeNull();

            var bcs = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            var chain = await bcs.GetChainAsync();
            var transactions = (await plugin.GetPreTransactionsAsync(TestContractContainer.Descriptors,
                new TransactionContext
                {
                    Transaction = new Transaction
                    {
                        From = DefaultAddress,
                        To = _testContractAddress,
                        MethodName = nameof(_testContractStub.TestMethod)
                    },
                    BlockHeight = chain.BestChainHeight + 1,
                    PreviousBlockHash = chain.BestChainHash
                })).ToList();
    
            transactions.ShouldNotBeEmpty();
            transactions[0].From.ShouldBe(DefaultAddress);
            transactions[0].To.ShouldBe(TokenContractAddress);
        }
    }

    [Fact]
    public async Task ChargeUserFeeTest_Success()
    {
        await Initialize();
        var beforeBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultAddress,
            Symbol = "ELF"
        });
        var result = await _testContractStub.TestMethod.SendAsync(new Empty());
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var transactionFeeDic = result.TransactionResult.GetChargedTransactionFees();
        await CheckTransactionFeesMapAsync(transactionFeeDic);

        var after = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultAddress,
            Symbol = "ELF"
        });
        after.Balance.ShouldBe(beforeBalance.Balance - transactionFeeDic[beforeBalance.Symbol]);
    }

    [Fact]
    public async Task ChargeUserFeeTest_Success_IsFree()
    {
        await DeployTestContractAsync();
        var beforeBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultAddress,
            Symbol = "ELF"
        });
        var result = await _testContractStub.TestMethod.SendAsync(new Empty());
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var transactionFeeDic = result.TransactionResult.GetChargedTransactionFees();
        await CheckTransactionFeesMapAsync(transactionFeeDic);

        var after = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultAddress,
            Symbol = "ELF"
        });
        after.Balance.ShouldBe(beforeBalance.Balance);
    }
    
    [Fact]
    public async Task GetPreTransactions_None_PreTransaction_Test()
    {
        await Initialize();
        var plugin = GetCreateInstance<IPreExecutionPlugin, UserFeeChargePreExecutionPlugin>();
        plugin.ShouldNotBeNull();
        var bcs = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
        var chain = await bcs.GetChainAsync();

        //height == 1
        var transactions = (await plugin.GetPreTransactionsAsync(TestContractContainer.Descriptors,
            new TransactionContext
            {
                Transaction = new Transaction
                {
                    From = DefaultAddress,
                    To = _testContractAddress,
                    MethodName = nameof(_testContractStub.TestMethod)
                },
                BlockHeight = 1,
                PreviousBlockHash = chain.BestChainHash
            })).ToList();

        transactions.Count.ShouldBe(0);

        // invalid contract descriptor
        transactions = (await plugin.GetPreTransactionsAsync(AEDPoSContractContainer.Descriptors,
            new TransactionContext
            {
                Transaction = new Transaction
                {
                    From = DefaultAddress,
                    To = DefaultAddress,
                    MethodName = nameof(_testContractStub.TestMethod)
                },
                BlockHeight = chain.BestChainHeight + 1,
                PreviousBlockHash = chain.BestChainHash
            })).ToList();

        transactions.Count.ShouldBe(0);

        // method name == ChargeTransactionFees, to == token contract address
        transactions = (await plugin.GetPreTransactionsAsync(AEDPoSContractContainer.Descriptors,
            new TransactionContext
            {
                Transaction = new Transaction
                {
                    From = DefaultAddress,
                    To = TokenContractAddress,
                    MethodName = nameof(TokenContractContainer.TokenContractStub.ChargeTransactionFees)
                },
                BlockHeight = chain.BestChainHeight + 1,
                PreviousBlockHash = chain.BestChainHash
            })).ToList();

        transactions.Count.ShouldBe(0);
    }
    
    private async Task DeployTestContractAsync()
    {
        var category = KernelConstants.CodeCoverageRunnerCategory;
        var code = Codes.Single(kv => kv.Key.Contains("ExecutionPluginForUserFee.Tests.TestContract")).Value;
        _testContractAddress = await DeploySystemSmartContract(category, code,
            HashHelper.ComputeFrom("ExecutionPluginForUserFee.Tests.TestContract"),
            DefaultSenderKeyPair);
        _testContractStub =
            GetTester<TestContractContainer.TestContractStub>(_testContractAddress, DefaultSenderKeyPair);
    }

    private async Task Initialize()
    {
        await DeployTestContractAsync();
        
        {
            var proposalId = await SetUserFeeAsync(1_00000000);
            await ParliamentContractStub.Approve.SendAsync(proposalId);
            var releaseRet = await ParliamentContractStub.Release.SendAsync(proposalId);
            releaseRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var configuration = await ConfigurationStub.GetConfiguration.CallAsync(new StringValue
            {
                Value = ConfigurationKey
            });
            var fee = new UserMethodFees();
            fee.MergeFrom(configuration.Value);
            fee.Fees.First().Symbol.ShouldBe("ELF");
            fee.Fees.First().BasicFee.ShouldBe(1_00000000);
        }
    }
    
    private async Task CheckTransactionFeesMapAsync(Dictionary<string, long> transactionFeeDic)
    {
        var chain = await _blockchainService.GetChainAsync();
        var transactionFeesMap = await _totalTransactionFeesMapProvider.GetTotalTransactionFeesMapAsync(new ChainContext
        {
            BlockHash = chain.BestChainHash,
            BlockHeight = chain.BestChainHeight
        });
        foreach (var transactionFee in transactionFeeDic)
            transactionFeesMap.Value[transactionFee.Key].ShouldBe(transactionFee.Value);
    }
    private async Task<Hash> SetUserFeeAsync(int amount)
    {
        var createProposalInput = SetUserFee(amount);
        var organizationAddress = await GetParliamentDefaultOrganizationAddressAsync();
        var proposalId =
            await CreateProposalAsync(organizationAddress, createProposalInput, "SetConfiguration");
        return proposalId;
    }
    private async Task<Address> GetParliamentDefaultOrganizationAddressAsync()
    {
        var organizationAddress = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        return organizationAddress;
    }
    private async Task<Hash> CreateProposalAsync(Address organizationAddress, IMessage input, string methodName)
    {
        var result = await AuthorizationContractStub.CreateProposal.SendAsync(new CreateProposalInput
        {
            ContractMethodName = methodName,
            ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
            Params = input.ToByteString(),
            ToAddress = ConfigurationAddress,
            OrganizationAddress = organizationAddress
        });
        var proposalId = Hash.Parser.ParseFrom(result.TransactionResult.ReturnValue);
        return proposalId;
    }
    private SetConfigurationInput SetUserFee(int amount)
    {
        var transactionFee = new UserMethodFees()
        {
            Fees=
            {
                new UserMethodFee
                {
                    Symbol = "ELF",
                    BasicFee = amount
                }
            },
            IsSizeFeeFree = false
        };
        return new SetConfigurationInput
        {
            Key = ConfigurationKey,
            Value = transactionFee.ToByteString()
        };
    }
    private I GetCreateInstance<I, T>() where T : I
    {
        var implements = Application.ServiceProvider.GetRequiredService<IEnumerable<I>>()
            .ToLookup(p => p.GetType()).Select(coll => coll.First()); // One instance per t
        var implement = implements.SingleOrDefault(p => p.GetType() == typeof(T));
        return implement;
    }
}