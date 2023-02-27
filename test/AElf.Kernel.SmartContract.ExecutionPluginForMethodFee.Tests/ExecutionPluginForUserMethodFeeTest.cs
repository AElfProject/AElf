using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Configuration;
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

    private async Task DeployTestContractAsync()
    {
        var category = KernelConstants.CodeCoverageRunnerCategory;
        var code = Codes.Single(kv => kv.Key.Contains("ExecutionPluginForSystemFee.Tests.TestContract")).Value;
        _testContractAddress = await DeploySystemSmartContract(category, code,
            HashHelper.ComputeFrom("ExecutionPluginForSystemFee.Tests.TestContract"),
            DefaultSenderKeyPair);
        _testContractStub =
            GetTester<TestContractContainer.TestContractStub>(_testContractAddress, DefaultSenderKeyPair);
    }
    private async Task<Address> GetTokenContractAddressAsync()
    {
        var preBlockHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync();
        var chainContext = new ChainContext
        {
            BlockHash = preBlockHeader.GetHash(),
            BlockHeight = preBlockHeader.Height
        };
        var contractMapping =
            await ContractAddressService.GetSystemContractNameToAddressMappingAsync(chainContext);

        return contractMapping[TokenSmartContractAddressNameProvider.Name];
    }
    private async Task<TokenContractContainer.TokenContractStub> GetTokenContractStubAsync()
    {
        var tokenContractAddress = await GetTokenContractAddressAsync();
        var tokenStub = GetTester<TokenContractContainer.TokenContractStub>(
            tokenContractAddress, DefaultSenderKeyPair);

        return tokenStub;
    }

    private async Task Initialize()
    {
        await DeployTestContractAsync();
        
        {
            var proposalId = await SetSystemFeeAsync(1_00000000);
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

    [Fact]
    public async Task GetPreTransactionFeeTest()
    {
        await Initialize();
        {
            var plugin = GetCreateInstance<IPreExecutionPlugin, SystemFeeChargePreExecutionPlugin>();
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
            transactions[0].To.ShouldBe(await GetTokenContractAddressAsync());
        }
    }

    [Fact]
    public async Task ChargeSystemFeeTest_Success()
    {
        await Initialize();
        TokenContractStub = await GetTokenContractStubAsync();
        await SetPrimaryTokenSymbolAsync(TokenContractStub);
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
    private async Task SetPrimaryTokenSymbolAsync(TokenContractContainer.TokenContractStub tokenContractStub)
    {
        await tokenContractStub.SetPrimaryTokenSymbol.SendAsync(new SetPrimaryTokenSymbolInput
            { Symbol = "ELF" });
    }
    internal async Task<Hash> SetSystemFeeAsync(int amount)
    {
        var createProposalInput = SetSystemFee(amount);
        var organizationAddress = await GetParliamentDefaultOrganizationAddressAsync();
        var proposalId =
            await CreateProposalAsync(organizationAddress, createProposalInput, "SetConfiguration");
        return proposalId;
    }
    internal async Task<Address> GetParliamentDefaultOrganizationAddressAsync()
    {
        var organizationAddress = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        return organizationAddress;
    }
    internal async Task<Hash> CreateProposalAsync(Address organizationAddress, IMessage input, string methodName)
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
    internal SetConfigurationInput SetSystemFee(int amount)
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