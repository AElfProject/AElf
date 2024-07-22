using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Configuration;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Application;

public sealed class SmartContractExecutiveServiceTests : SmartContractTestBase
{
    private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;
    private readonly KernelTestHelper _kernelTestHelper;
    private readonly ISmartContractExecutiveProvider _smartContractExecutiveProvider;
    private readonly SmartContractExecutiveService _smartContractExecutiveService;
    private readonly SmartContractHelper _smartContractHelper;
    private readonly ISmartContractRegistrationProvider _smartContractRegistrationProvider;
    private readonly ITransactionResultQueryService _transactionResultQueryService;

    public SmartContractExecutiveServiceTests()
    {
        _smartContractExecutiveService = GetRequiredService<SmartContractExecutiveService>();
        _smartContractHelper = GetRequiredService<SmartContractHelper>();
        _smartContractRegistrationProvider = GetRequiredService<ISmartContractRegistrationProvider>();
        _defaultContractZeroCodeProvider = GetRequiredService<IDefaultContractZeroCodeProvider>();
        _smartContractExecutiveProvider = GetRequiredService<ISmartContractExecutiveProvider>();
        _transactionResultQueryService = GetRequiredService<ITransactionResultQueryService>();
        _kernelTestHelper = GetRequiredService<KernelTestHelper>();
    }

    [Fact]
    public void GetExecutive_ThrowArgumentNullException()
    {
        _smartContractExecutiveService.GetExecutiveAsync(new ChainContext
        {
            BlockHash = Hash.Empty,
            BlockHeight = 0
        }, null).ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public async Task GetExecutive_ThrowSmartContractFindRegistrationException()
    {
        var chain = await _smartContractHelper.CreateChainWithGenesisContractAsync();
        _smartContractExecutiveService.GetExecutiveAsync(new ChainContext
        {
            BlockHash = chain.BestChainHash,
            BlockHeight = chain.BestChainHeight
        }, SampleAddress.AddressList[0]).ShouldThrow<SmartContractFindRegistrationException>();
    }

    [Fact]
    public async Task GetExecutive_With_SmartContractRegistrationProvider_Test()
    {
        var chain = await _smartContractHelper.CreateChainWithGenesisContractAsync();

        var chainContext = new ChainContext
        {
            BlockHash = chain.BestChainHash,
            BlockHeight = chain.BestChainHeight
        };
        //Get executive by smartContractRegistration in SmartContractRegistrationProvider
        await _smartContractRegistrationProvider.SetSmartContractRegistrationAsync(chainContext,
            _defaultContractZeroCodeProvider.ContractZeroAddress,
            _defaultContractZeroCodeProvider.DefaultContractZeroRegistration);
        var executive = await _smartContractExecutiveService
            .GetExecutiveAsync(chainContext, _defaultContractZeroCodeProvider.ContractZeroAddress);
        executive.ContractHash.ShouldBe(_defaultContractZeroCodeProvider.DefaultContractZeroRegistration.CodeHash);

        await _smartContractExecutiveService.PutExecutiveAsync(chainContext,
            _defaultContractZeroCodeProvider.ContractZeroAddress, executive);

        //Get executive from executive pool
        executive = await _smartContractExecutiveService
            .GetExecutiveAsync(chainContext, _defaultContractZeroCodeProvider.ContractZeroAddress);
        executive.ContractHash.ShouldBe(_defaultContractZeroCodeProvider.DefaultContractZeroRegistration.CodeHash);

        var otherExecutive = await _smartContractExecutiveService
            .GetExecutiveAsync(chainContext, _defaultContractZeroCodeProvider.ContractZeroAddress);

        await _smartContractExecutiveService.PutExecutiveAsync(chainContext,
            _defaultContractZeroCodeProvider.ContractZeroAddress, executive);

        await _smartContractExecutiveService.PutExecutiveAsync(chainContext,
            _defaultContractZeroCodeProvider.ContractZeroAddress, otherExecutive);

        _smartContractExecutiveProvider.GetExecutivePools()[_defaultContractZeroCodeProvider.ContractZeroAddress]
            .Count.ShouldBe(2);

        //Make codeHash different between smartContractRegistration and executive 
        var code = _smartContractHelper.Codes["AElf.Contracts.Configuration"];
        var codeHash = HashHelper.ComputeFrom(code);
        await _smartContractRegistrationProvider.SetSmartContractRegistrationAsync(chainContext,
            _defaultContractZeroCodeProvider.ContractZeroAddress, new SmartContractRegistration
            {
                Category = KernelConstants.DefaultRunnerCategory,
                Code = ByteString.CopyFrom(code),
                CodeHash = HashHelper.ComputeFrom(code)
            });

        executive = await _smartContractExecutiveService
            .GetExecutiveAsync(chainContext, _defaultContractZeroCodeProvider.ContractZeroAddress);
        executive.ContractHash.ShouldBe(codeHash);
        _smartContractExecutiveProvider.GetExecutivePools()
            .TryGetValue(_defaultContractZeroCodeProvider.ContractZeroAddress, out _).ShouldBeFalse();
    }

    [Fact]
    public async Task GetExecutive_With_GenesisContract_Test()
    {
        var chain = await _smartContractHelper.CreateChainWithGenesisContractAsync();

        var chainContext = new ChainContext
        {
            BlockHash = chain.BestChainHash,
            BlockHeight = chain.BestChainHeight
        };
        //Get executive of genesis contract by smartContractRegistration in genesis contract and blockHeight=1
        var executive = await _smartContractExecutiveService
            .GetExecutiveAsync(chainContext, _defaultContractZeroCodeProvider.ContractZeroAddress);
        executive.ContractHash.ShouldBe(_defaultContractZeroCodeProvider.DefaultContractZeroRegistration.CodeHash);

        var transaction = _smartContractHelper.BuildDeploySystemSmartContractTransaction(
            ConfigurationSmartContractAddressNameProvider.Name,
            _smartContractHelper.Codes["AElf.Contracts.Configuration"]);
        var block = await _smartContractHelper.GenerateBlockAsync(chain.BestChainHeight, chain.BestChainHash,
            new List<Transaction>
            {
                transaction
            });
        await _smartContractHelper.MineBlockAsync(block);

        chainContext = new ChainContext
        {
            BlockHash = block.GetHash(),
            BlockHeight = block.Height
        };

        var transactionResult =
            await _transactionResultQueryService.GetTransactionResultAsync(transaction.GetHash(), block.GetHash());
        var contractDeployed = new ContractDeployed();
        contractDeployed.MergeFrom(transactionResult.Logs.First(l => l.Name == nameof(ContractDeployed)));

        //Get executive of configuration contract in genesis contract
        executive = await _smartContractExecutiveService.GetExecutiveAsync(chainContext, contractDeployed.Address);
        executive.ContractHash.ShouldBe(
            HashHelper.ComputeFrom(_smartContractHelper.Codes["AElf.Contracts.Configuration"]));

        //Get executive of genesis contract by smartContractRegistration in genesis contract and blockHeight=2
        executive = await _smartContractExecutiveService
            .GetExecutiveAsync(chainContext, _defaultContractZeroCodeProvider.ContractZeroAddress);
        executive.ContractHash.ShouldBe(_defaultContractZeroCodeProvider.DefaultContractZeroRegistration.CodeHash);
    }

    [Fact]
    public async Task CleanExecutive_Test()
    {
        await _smartContractHelper.CreateChainWithGenesisContractAsync();

        var address = _defaultContractZeroCodeProvider.ContractZeroAddress;
        _smartContractExecutiveProvider.TryGetValue(address, out var pool).ShouldBeTrue();
        pool.Count.ShouldBe(1);
        _smartContractExecutiveService.CleanExecutive(_defaultContractZeroCodeProvider.ContractZeroAddress);
        _smartContractExecutiveProvider.TryGetValue(address, out _).ShouldBeFalse();
    }

    [Fact]
    public async Task CleanIdleExecutive_Test()
    {
        var chain = await _smartContractHelper.CreateChainWithGenesisContractAsync();

        var address = _defaultContractZeroCodeProvider.ContractZeroAddress;
        _smartContractExecutiveProvider.TryGetValue(address, out var pool).ShouldBeTrue();
        pool.Count.ShouldBe(1);
        _smartContractExecutiveService.CleanIdleExecutive();
        _smartContractExecutiveProvider.TryGetValue(address, out pool).ShouldBeTrue();
        pool.Count.ShouldBe(1);

        var chainContext = new ChainContext
        {
            BlockHash = chain.BestChainHash,
            BlockHeight = chain.BestChainHeight
        };

        var executives = new List<IExecutive>();
        for (var i = 0; i < 51; i++)
        {
            var executive = await _smartContractExecutiveService.GetExecutiveAsync(chainContext, address);
            executives.Add(executive);
        }

        foreach (var executive in executives)
            await _smartContractExecutiveService.PutExecutiveAsync(chainContext, address, executive);
        _smartContractExecutiveProvider.TryGetValue(address, out pool);
        pool.Count.ShouldBe(51);
        _smartContractExecutiveService.CleanIdleExecutive();
        pool.Count.ShouldBe(50);
        _smartContractExecutiveProvider.TryGetValue(address, out pool);
        pool.Count.ShouldBe(50);

        pool.TryPeek(out var item);
        item.LastUsedTime = item.LastUsedTime.AddHours(-2);
        _smartContractExecutiveService.CleanIdleExecutive();
        _smartContractExecutiveProvider.TryGetValue(address, out pool);
        pool.Count.ShouldBe(49);

        pool.TryPeek(out item);
        item.LastUsedTime = item.LastUsedTime.AddHours(-2);
        _smartContractExecutiveService.CleanIdleExecutive();
        _smartContractExecutiveProvider.TryGetValue(address, out pool);
        pool.Count.ShouldBe(48);
    }

    [Fact]
    public async Task Put_Executive_Success()
    {
        var chain = await _smartContractHelper.CreateChainWithGenesisContractAsync();
        var chainContext = new ChainContext
        {
            BlockHash = chain.BestChainHash,
            BlockHeight = chain.BestChainHeight
        };
        var address = _defaultContractZeroCodeProvider.ContractZeroAddress;
        var executive = await _smartContractExecutiveService
            .GetExecutiveAsync(chainContext, address);
        _smartContractExecutiveProvider.TryGetValue(address, out var pool);
        pool.Count.ShouldBe(0);

        var code = _smartContractHelper.Codes["AElf.Contracts.Configuration"];
        await _smartContractRegistrationProvider.SetSmartContractRegistrationAsync(chainContext,
            address, new SmartContractRegistration
            {
                Category = KernelConstants.DefaultRunnerCategory,
                Code = ByteString.CopyFrom(code),
                CodeHash = HashHelper.ComputeFrom(code)
            });
        await _smartContractExecutiveService.PutExecutiveAsync(chainContext, address, executive);
        _smartContractExecutiveProvider.TryGetValue(address, out pool);
        pool.Count.ShouldBe(1);

        await _smartContractRegistrationProvider.SetSmartContractRegistrationAsync(chainContext, address,
            _defaultContractZeroCodeProvider.DefaultContractZeroRegistration);

        var block = await _kernelTestHelper.AttachBlockToBestChain();
        chainContext.BlockHash = block.GetHash();
        chainContext.BlockHeight = block.Height;
        pool.TryTake(out _);
        pool.Count.ShouldBe(0);
        await _smartContractExecutiveService.PutExecutiveAsync(chainContext, address, executive);
        pool.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Put_Executive_Failed()
    {
        var chain = await _smartContractHelper.CreateChainWithGenesisContractAsync();
        var chainContext = new ChainContext
        {
            BlockHash = chain.BestChainHash,
            BlockHeight = chain.BestChainHeight
        };
        var address = _defaultContractZeroCodeProvider.ContractZeroAddress;
        var executive = await _smartContractExecutiveService
            .GetExecutiveAsync(chainContext, address);
        _smartContractExecutiveProvider.TryGetValue(address, out var pool);
        pool.Count.ShouldBe(0);
        await _smartContractExecutiveService.PutExecutiveAsync(chainContext, SampleAddress.AddressList[0], executive);
        _smartContractExecutiveProvider.TryGetValue(address, out pool);
        pool.Count.ShouldBe(0);

        var block = await _kernelTestHelper.AttachBlockToBestChain();
        chainContext.BlockHash = block.GetHash();
        chainContext.BlockHeight = block.Height;
        await _smartContractExecutiveService.PutExecutiveAsync(chainContext, address, executive);
        _smartContractExecutiveProvider.TryGetValue(address, out pool);
        pool.Count.ShouldBe(0);

        var code = _smartContractHelper.Codes["AElf.Contracts.Configuration"];
        await _smartContractRegistrationProvider.SetSmartContractRegistrationAsync(chainContext,
            address, new SmartContractRegistration
            {
                Category = KernelConstants.DefaultRunnerCategory,
                Code = ByteString.CopyFrom(code),
                CodeHash = HashHelper.ComputeFrom(code)
            });
        await _smartContractExecutiveService.PutExecutiveAsync(chainContext, address, executive);
        _smartContractExecutiveProvider.TryGetValue(address, out pool);
        pool.Count.ShouldBe(0);
    }
}