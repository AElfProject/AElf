using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Configuration;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContractExecution.Application;

public sealed class LogEventProcessorTests : SmartContractExecutionTestBase
{
    private readonly IBlockchainService _blockchainService;
    private readonly CodeUpdatedLogEventProcessor _codeUpdatedLogEventProcessor;
    private readonly ContractDeployedLogEventProcessor _contractDeployedLogEventProcessor;
    private readonly ISmartContractAddressProvider _smartContractAddressProvider;
    private readonly ISmartContractAddressService _smartContractAddressService;
    private readonly SmartContractExecutionHelper _smartContractExecutionHelper;
    private readonly ISmartContractRegistrationProvider _smartContractRegistrationProvider;
    private readonly ITransactionResultQueryService _transactionResultQueryService;

    public LogEventProcessorTests()
    {
        _contractDeployedLogEventProcessor = GetRequiredService<ContractDeployedLogEventProcessor>();
        _codeUpdatedLogEventProcessor = GetRequiredService<CodeUpdatedLogEventProcessor>();
        _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
        _smartContractExecutionHelper = GetRequiredService<SmartContractExecutionHelper>();
        _transactionResultQueryService = GetRequiredService<ITransactionResultQueryService>();
        _blockchainService = GetRequiredService<IBlockchainService>();
        _smartContractAddressProvider = GetRequiredService<ISmartContractAddressProvider>();
        _smartContractRegistrationProvider = GetRequiredService<ISmartContractRegistrationProvider>();
    }

    [Fact]
    public async Task GetInterestedEventAsync_Test()
    {
        var contractDeployedInterestedEvent =
            await _contractDeployedLogEventProcessor.GetInterestedEventAsync(new ChainContext());
        CheckInterestedEvent<ContractDeployed>(contractDeployedInterestedEvent);
        var contractDeployedInterestedEventInCache =
            await _contractDeployedLogEventProcessor.GetInterestedEventAsync(new ChainContext());
        CheckInterestedEvent<ContractDeployed>(contractDeployedInterestedEventInCache);

        var codeUpdatedInterestedEvent =
            await _codeUpdatedLogEventProcessor.GetInterestedEventAsync(new ChainContext());
        CheckInterestedEvent<CodeUpdated>(codeUpdatedInterestedEvent);
        var codeUpdatedInterestedEventInCache =
            await _codeUpdatedLogEventProcessor.GetInterestedEventAsync(new ChainContext());
        CheckInterestedEvent<CodeUpdated>(codeUpdatedInterestedEventInCache);
    }

    private void CheckInterestedEvent<T>(InterestedEvent interestedEvent) where T : IEvent<T>, new()
    {
        var zeroSmartContractAddress = _smartContractAddressService.GetZeroSmartContractAddress();
        var logEvent = new T().ToLogEvent(zeroSmartContractAddress);
        var contractDeployedEvent = new InterestedEvent
        {
            LogEvent = logEvent,
            Bloom = logEvent.GetBloom()
        };
        interestedEvent.LogEvent.ShouldBe(contractDeployedEvent.LogEvent);
        interestedEvent.Bloom.Data.ShouldBe(contractDeployedEvent.Bloom.Data);
    }

    [Fact]
    public async Task ProcessAsync_Test()
    {
        var chain = await _smartContractExecutionHelper.CreateChainAsync();
        var block = await _blockchainService.GetBlockByHashAsync(chain.BestChainHash);
        var tasks = block.Body.TransactionIds
            .Select(t => _transactionResultQueryService.GetTransactionResultAsync(t)).ToList();
        var transactionResultList = await Task.WhenAll(tasks);

        await ProcessTransactionResultsAsync(transactionResultList, block);

        var transaction = new Transaction
        {
            From = _smartContractAddressService.GetZeroSmartContractAddress(),
            To = _smartContractAddressService.GetZeroSmartContractAddress(),
            MethodName = nameof(ACS0Container.ACS0Stub.DeploySmartContract),
            Params = new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory,
                Code = ByteString.CopyFrom(
                    _smartContractExecutionHelper.ContractCodes["AElf.Contracts.MultiToken"])
            }.ToByteString()
        };
        var blockExecutedSet = await _smartContractExecutionHelper.ExecuteTransactionAsync(transaction);

        await ProcessTransactionResultsAsync(blockExecutedSet.TransactionResultMap.Values.ToArray(),
            blockExecutedSet.Block);

        await ProcessCodeUpdateAsync(chain);
    }

    private async Task ProcessCodeUpdateAsync(Chain chain)
    {
        var chainContext = new ChainContext
        {
            BlockHash = chain.BestChainHash,
            BlockHeight = chain.BestChainHeight
        };
        var address = await _smartContractAddressService.GetAddressByContractNameAsync(chainContext,
            ConfigurationSmartContractAddressNameProvider.StringName);
        var transaction = new Transaction
        {
            From = _smartContractAddressService.GetZeroSmartContractAddress(),
            To = _smartContractAddressService.GetZeroSmartContractAddress(),
            MethodName = nameof(ACS0Container.ACS0Stub.UpdateSmartContract),
            Params = new ContractUpdateInput
            {
                Code = ByteString.CopyFrom(
                    _smartContractExecutionHelper.ContractCodes["AElf.Contracts.TestContract.BasicFunction"]),
                Address = address
            }.ToByteString()
        };
        var blockExecutedSet = await _smartContractExecutionHelper.ExecuteTransactionAsync(transaction, chain);

        var interestedEvent = await _codeUpdatedLogEventProcessor.GetInterestedEventAsync(chainContext);
        foreach (var transactionResult in blockExecutedSet.TransactionResultMap.Values)
        {
            var logEvent = transactionResult.Logs.First(l =>
                l.Address == interestedEvent.LogEvent.Address && l.Name == interestedEvent.LogEvent.Name);
            var codeUpdated = new CodeUpdated();
            codeUpdated.MergeFrom(logEvent);
            var smartContractRegistration = await _smartContractRegistrationProvider.GetSmartContractRegistrationAsync(
                chainContext,
                codeUpdated.Address);

            await _codeUpdatedLogEventProcessor.ProcessAsync(blockExecutedSet.Block,
                new Dictionary<TransactionResult, List<LogEvent>>
                    { { transactionResult, new List<LogEvent> { logEvent } } });

            chainContext = new ChainContext
            {
                BlockHash = blockExecutedSet.Block.GetHash(),
                BlockHeight = blockExecutedSet.Block.Height
            };
            var updatedSmartContractRegistration =
                await _smartContractRegistrationProvider.GetSmartContractRegistrationAsync(chainContext,
                    codeUpdated.Address);
            updatedSmartContractRegistration.ShouldNotBe(smartContractRegistration);
            updatedSmartContractRegistration.Code.ShouldBe(ByteString.CopyFrom(
                _smartContractExecutionHelper.ContractCodes["AElf.Contracts.TestContract.BasicFunction"]));
        }
    }

    private async Task ProcessTransactionResultsAsync(TransactionResult[] transactionResultList, Block block)
    {
        var chainContext = new ChainContext
        {
            BlockHash = block.GetHash(),
            BlockHeight = block.Height
        };
        var interestedEvent = await _contractDeployedLogEventProcessor.GetInterestedEventAsync(chainContext);
        foreach (var transactionResult in transactionResultList)
        {
            var logEvent = transactionResult.Logs.First(l =>
                l.Address == interestedEvent.LogEvent.Address && l.Name == interestedEvent.LogEvent.Name);
            var contractDeployed = new ContractDeployed();
            contractDeployed.MergeFrom(logEvent);
            if (contractDeployed.Name != null)
            {
                var smartContractAddress = await _smartContractAddressProvider.GetSmartContractAddressAsync(
                    chainContext,
                    contractDeployed.Name.ToStorageKey());
                smartContractAddress.ShouldBeNull();
            }

            var smartContractRegistration = await _smartContractRegistrationProvider.GetSmartContractRegistrationAsync(
                chainContext,
                contractDeployed.Address);
            smartContractRegistration.ShouldBeNull();

            await _contractDeployedLogEventProcessor.ProcessAsync(block,
                new Dictionary<TransactionResult, List<LogEvent>>
                    { { transactionResult, new List<LogEvent> { logEvent } } });

            smartContractRegistration = await _smartContractRegistrationProvider.GetSmartContractRegistrationAsync(
                chainContext,
                contractDeployed.Address);
            smartContractRegistration.ShouldNotBeNull();
            if (contractDeployed.Name == null) continue;
            {
                var smartContractAddress = await _smartContractAddressProvider.GetSmartContractAddressAsync(
                    chainContext,
                    contractDeployed.Name.ToStorageKey());
                smartContractAddress.ShouldNotBeNull();
            }
        }
    }
}