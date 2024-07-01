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
using AElf.Kernel.SmartContract.ExecutionPluginForUserContractFee.Tests.TestContract;
using AElf.Standards.ACS12;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests;

public class ExecutionPluginForUserContractContractMethodFeeTest : ExecutionPluginForUserContractMethodFeeTestBase
{
    private Address _testContractAddress;
    private TestContractContainer.TestContractStub _testContractStub;
    private readonly IBlockchainService _blockchainService;
    private readonly ITotalTransactionFeesMapProvider _totalTransactionFeesMapProvider;
    private const string ConfigurationKey = "UserContractMethodFee";

    public ExecutionPluginForUserContractContractMethodFeeTest()
    {
        _blockchainService = GetRequiredService<IBlockchainService>();
        _totalTransactionFeesMapProvider = GetRequiredService<ITotalTransactionFeesMapProvider>();
    }

    [Fact]
    public async Task GetPreTransactionFeeTest()
    {
        await Initialize();
        {
            var plugin = GetCreateInstance<IPreExecutionPlugin, UserContractFeeChargePreExecutionPlugin>();
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
    public async Task ChargeUserContractFeeTest_Success()
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
        await CheckTransactionFeesMapAsync(DefaultAddress,transactionFeeDic);

        var after = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultAddress,
            Symbol = "ELF"
        });
        after.Balance.ShouldBe(beforeBalance.Balance - transactionFeeDic[DefaultAddress][beforeBalance.Symbol]);
    }

    [Fact]
    public async Task ChargeUserContractFeeTest_Success_BaseFeeIsFree()
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
        await CheckTransactionFeesMapAsync(DefaultAddress,transactionFeeDic);

        var after = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultAddress,
            Symbol = "ELF"
        });
        // - size fee
        after.Balance.ShouldBe(beforeBalance.Balance - 20135000);
    }

    [Fact]
    public async Task GetPreTransactions_None_PreTransaction_Test()
    {
        await Initialize();
        var plugin = GetCreateInstance<IPreExecutionPlugin, UserContractFeeChargePreExecutionPlugin>();
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
    }

    [Theory]
    [InlineData(100000000, 0, 3, 10, 1, 2, "ELF", 20135010, true)]
    [InlineData(9, 0, 1, 10, 1, 2, "ELF", 9, false)]
    [InlineData(100000000, 2, 2, 0, 1, 2, "TSA", 1, true)]
    [InlineData(100000000, 2, 2, 0, 13, 2, "TSB", 2, true)]
    [InlineData(100000000, 2, 2, 0, 20, 20, "TSA", 2, false)]
    [InlineData(1, 0, 1, 0, 1, 2, "TSB", 1, false)]
    [InlineData(10, 0, 0, 0, 1, 2, "ELF", 10, false)] // Charge 10 ELFs tx size fee.
    public async Task ChargeFee_SetConfiguration_Tests(long balance1, long balance2, long balance3, long fee1,
        long fee2, long fee3, string chargedSymbol, long chargedAmount, bool isChargingSuccessful)
    {
        await DeployTestContractAsync();
        await TokenContractStub.Transfer.SendAsync(new TransferInput
        {
            Symbol = "ELF",
            Amount = balance1,
            To = Accounts[1].Address,
            Memo = "Set for token converter."
        });
        await CreateAndIssueTokenAsync("TSA", balance2, Accounts[1].Address);
        await CreateAndIssueTokenAsync("TSB", balance3, Accounts[1].Address);

        var transactionFee = new UserContractMethodFees();
        if (fee1 > 0)
            transactionFee.Fees.Add(new UserContractMethodFee {Symbol = "ELF", BasicFee = fee1});
        if (fee2 > 0)
            transactionFee.Fees.Add(new UserContractMethodFee {Symbol = "TSA", BasicFee = fee2});
        if (fee3 > 0)
            transactionFee.Fees.Add(new UserContractMethodFee {Symbol = "TSB", BasicFee = fee3});
        var createProposalInput = new SetConfigurationInput
        {
            Key = ConfigurationKey,
            Value = transactionFee.ToByteString()
        };

        await ConfigurationStub.SetConfiguration.SendAsync(createProposalInput);

        var originBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = Accounts[1].Address,
            Symbol = chargedSymbol ?? "ELF"
        })).Balance;

        Dictionary<Address,Dictionary<string, long>> transactionFeeDic;
        var userTestContractStub =
            GetTester<TestContractContainer.TestContractStub>(_testContractAddress, Accounts[1].KeyPair);
        if (isChargingSuccessful)
        {
            var result = await userTestContractStub.TestMethod.SendAsync(new Empty());
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            if (chargedSymbol != null)
            {
                var token = new Dictionary<string, long>
                {
                    [chargedSymbol] = chargedAmount
                };
                result.TransactionResult.GetChargedTransactionFees().Keys.ShouldContain(Accounts[1].Address);
                var fee = result.TransactionResult.GetChargedTransactionFees()[Accounts[1].Address];
                fee.ShouldContainKeyAndValue(chargedSymbol,chargedAmount);
            }

            transactionFeeDic = result.TransactionResult.GetChargedTransactionFees();
        }
        else
        {
            var result = await userTestContractStub.TestMethod.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.ShouldBe("Pre-Error: Transaction fee not enough.");
            if (chargedSymbol != null)
            {
                var token = new Dictionary<string, long>
                {
                    [chargedSymbol] = chargedAmount
                };
                result.TransactionResult.GetChargedTransactionFees().Keys.ShouldContain(Accounts[1].Address);
                var fee = result.TransactionResult.GetChargedTransactionFees()[Accounts[1].Address];
                fee.ShouldContainKeyAndValue(chargedSymbol,chargedAmount);
            }
            transactionFeeDic = result.TransactionResult.GetChargedTransactionFees();
        }

        await CheckTransactionFeesMapAsync(Accounts[1].Address,transactionFeeDic);
        if (chargedSymbol != null)
            transactionFeeDic[Accounts[1].Address][chargedSymbol].ShouldBe(chargedAmount);

        var finalBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = Accounts[1].Address,
            Symbol = chargedSymbol ?? "ELF"
        })).Balance;

        (originBalance - finalBalance).ShouldBe(chargedAmount);
    }

    [Fact]
    public async Task ChargeFee_SizeFeeIsFree()
    {
        await DeployTestContractAsync();
        var transactionFee = new UserContractMethodFees
        {
            IsSizeFeeFree = true
        };
        var createProposalInput = new SetConfigurationInput
        {
            Key = ConfigurationKey,
            Value = transactionFee.ToByteString()
        };
        await ConfigurationStub.SetConfiguration.SendAsync(createProposalInput);
        var beforeBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultAddress,
            Symbol = "ELF"
        });
        var result = await _testContractStub.TestMethod.SendAsync(new Empty());
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var transactionFeeDic = result.TransactionResult.GetChargedTransactionFees();
        await CheckTransactionFeesMapAsync(DefaultAddress, transactionFeeDic);

        var after = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultAddress,
            Symbol = "ELF"
        });
        // size fee = 0
        after.Balance.ShouldBe(beforeBalance.Balance);
    }

    [Fact]
    public async Task ChargeFee_SpecConfigurationFee()
    {
        await DeployTestContractAsync();
        var transactionFee = new UserContractMethodFees
        {
            Fees =
            {
                new UserContractMethodFee
                {
                    Symbol = "ELF",
                    BasicFee = 20_00000000
                }
            },
            IsSizeFeeFree = false
        };
        var createProposalInput = new SetConfigurationInput
        {
            Key =
                $"{ConfigurationKey}_{_testContractAddress.ToBase58()}_{nameof(TestContractContainer.TestContractStub.TestMethod)}",
            Value = transactionFee.ToByteString()
        };
        await ConfigurationStub.SetConfiguration.SendAsync(createProposalInput);
        var beforeBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultAddress,
            Symbol = "ELF"
        });
        var result = await _testContractStub.TestMethod.SendAsync(new Empty());
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var transactionFeeDic = result.TransactionResult.GetChargedTransactionFees();
        await CheckTransactionFeesMapAsync(DefaultAddress, transactionFeeDic);

        var after = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultAddress,
            Symbol = "ELF"
        });
        // - size fee
        after.Balance.ShouldBe(beforeBalance.Balance - 20135000 - 20_00000000);
    }

    private async Task CreateAndIssueTokenAsync(string symbol, long issueAmount, Address to)
    {
        await TokenContractStub.Create.SendAsync(new CreateInput
        {
            Symbol = symbol,
            Decimals = 2,
            IsBurnable = true,
            TokenName = "test token",
            TotalSupply = 1_000_000_00000000L,
            Issuer = DefaultAddress,
            Owner = DefaultAddress,
        });

        if (issueAmount != 0)
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = symbol,
                Amount = issueAmount,
                To = to,
                Memo = "Set for token converter."
            });
    }

    private async Task DeployTestContractAsync()
    {
        var category = KernelConstants.CodeCoverageRunnerCategory;
        var code = Codes.Single(kv => kv.Key.Contains("ExecutionPluginForUserContractFee.Tests.TestContract")).Value;
        _testContractAddress = await DeploySystemSmartContract(category, code,
            HashHelper.ComputeFrom("ExecutionPluginForUserContractFee.Tests.TestContract"),
            DefaultSenderKeyPair);
        _testContractStub =
            GetTester<TestContractContainer.TestContractStub>(_testContractAddress, DefaultSenderKeyPair);
    }

    private async Task Initialize()
    {
        await DeployTestContractAsync();

        {
            await SetUserContractFeeAsync(1_00000000);
            var configuration = await ConfigurationStub.GetConfiguration.CallAsync(new StringValue
            {
                Value = ConfigurationKey
            });
            var fee = new UserContractMethodFees();
            fee.MergeFrom(configuration.Value);
            fee.Fees.First().Symbol.ShouldBe("ELF");
            fee.Fees.First().BasicFee.ShouldBe(1_00000000);
        }
    }

    private async Task CheckTransactionFeesMapAsync(Address chargingAddress, Dictionary<Address,Dictionary<string, long>> transactionFeeDic)
    {
        var chain = await _blockchainService.GetChainAsync();
        var transactionFeesMap = await _totalTransactionFeesMapProvider.GetTotalTransactionFeesMapAsync(new ChainContext
        {
            BlockHash = chain.BestChainHash,
            BlockHeight = chain.BestChainHeight
        });
        foreach (var transactionFee in transactionFeeDic)
        {
            transactionFee.Key.ShouldBe(chargingAddress);
            foreach (var value in transactionFee.Value)
            {
                transactionFeesMap.Value[value.Key].ShouldBe(value.Value);
            }
        }
            
    }

    private async Task SetUserContractFeeAsync(int amount)
    {
        var createProposalInput = SetUserContractFee(amount);
        await ConfigurationStub.SetConfiguration.SendAsync(createProposalInput);
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

    private SetConfigurationInput SetUserContractFee(int amount)
    {
        var transactionFee = new UserContractMethodFees()
        {
            Fees =
            {
                new UserContractMethodFee
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