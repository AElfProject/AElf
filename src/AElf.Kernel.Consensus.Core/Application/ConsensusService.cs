using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.SmartContract.Application;
using AElf.Standards.ACS4;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.Consensus.Application;

internal class ConsensusService : IConsensusService, ISingletonDependency
{
    private readonly IBlockTimeProvider _blockTimeProvider;
    private readonly IConsensusReaderContextService _consensusReaderContextService;
    private readonly IConsensusScheduler _consensusScheduler;

    private readonly IContractReaderFactory<ConsensusContractContainer.ConsensusContractStub>
        _contractReaderFactory;

    private readonly ITriggerInformationProvider _triggerInformationProvider;
    private ConsensusCommand _consensusCommand;

    private Timestamp _nextMiningTime;

    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;
    private readonly Histogram<long> _TriggerConsensusAsync;
    private readonly Histogram<long> _ValidateConsensusAfterExecutionAsync;
    private readonly Histogram<long> _GetConsensusExtraDataAsync;
    private readonly Histogram<long> _GenerateConsensusTransactionsAsync;
    private readonly Histogram<long> _ValidateConsensusBeforeExecutionAsync;

    public ConsensusService(IConsensusScheduler consensusScheduler,
        IContractReaderFactory<ConsensusContractContainer.ConsensusContractStub> contractReaderFactory,
        ITriggerInformationProvider triggerInformationProvider,
        IBlockTimeProvider blockTimeProvider, IConsensusReaderContextService consensusReaderContextService,
        Instrumentation instrumentation)
    {
        _contractReaderFactory = contractReaderFactory;
        _triggerInformationProvider = triggerInformationProvider;
        _blockTimeProvider = blockTimeProvider;
        _consensusReaderContextService = consensusReaderContextService;
        _consensusScheduler = consensusScheduler;

        Logger = NullLogger<ConsensusService>.Instance;
        LocalEventBus = NullLocalEventBus.Instance;

        _activitySource = instrumentation.ActivitySource;
        _meter = _meter = new Meter("AElf", "1.0.0");
        
        _TriggerConsensusAsync = _meter.CreateHistogram<long>("TriggerConsensusAsync.rt","ms","The rt of executed txs");
        _ValidateConsensusAfterExecutionAsync = _meter.CreateHistogram<long>("ValidateConsensusAfterExecutionAsync.rt","ms","The rt of executed txs");
        _GetConsensusExtraDataAsync = _meter.CreateHistogram<long>("GetConsensusExtraDataAsync.rt","ms","The rt of executed txs");
        _GenerateConsensusTransactionsAsync = _meter.CreateHistogram<long>("GenerateConsensusTransactionsAsync.rt","ms","The rt of executed txs");
        _ValidateConsensusBeforeExecutionAsync = _meter.CreateHistogram<long>("ValidateConsensusBeforeExecutionAsync.rt","ms","The rt of executed txs");
    }

    public ILocalEventBus LocalEventBus { get; set; }

    public ILogger<ConsensusService> Logger { get; set; }

    /// <summary>
    ///     Basically update the consensus scheduler with latest consensus command.
    /// </summary>
    /// <param name="chainContext"></param>
    /// <returns></returns>
    [Ump]
    public async Task TriggerConsensusAsync(ChainContext chainContext)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        stopwatch.Start();
        using var activity = _activitySource.StartActivity();
        var now = TimestampHelper.GetUtcNow();
        _blockTimeProvider.SetBlockTime(now, chainContext.BlockHash);

        Logger.LogDebug($"Block time of triggering consensus: {now.ToDateTime():hh:mm:ss.ffffff}.");

        var triggerInformation =
            _triggerInformationProvider.GetTriggerInformationForConsensusCommand(new BytesValue());

        Logger.LogDebug($"Mining triggered, chain context: {chainContext.BlockHeight} - {chainContext.BlockHash}");

        // Upload the consensus command.
        var contractReaderContext =
            await _consensusReaderContextService.GetContractReaderContextAsync(chainContext);
        _consensusCommand = await _contractReaderFactory
            .Create(contractReaderContext).GetConsensusCommand
            .CallAsync(triggerInformation);

        if (_consensusCommand == null)
        {
            Logger.LogWarning("Consensus command is null.");
            return;
        }

        Logger.LogDebug($"Updated consensus command: {_consensusCommand}");

        // Update next mining time, also block time of both getting consensus extra data and txs.
        _nextMiningTime = _consensusCommand.ArrangedMiningTime;
        var leftMilliseconds = _consensusCommand.ArrangedMiningTime - TimestampHelper.GetUtcNow();
        leftMilliseconds = leftMilliseconds.Seconds > ConsensusConstants.MaximumLeftMillisecondsForNextBlock
            ? new Duration { Seconds = ConsensusConstants.MaximumLeftMillisecondsForNextBlock }
            : leftMilliseconds;

        // Update consensus scheduler.
        var blockMiningEventData = new ConsensusRequestMiningEventData(chainContext.BlockHash,
            chainContext.BlockHeight,
            _nextMiningTime,
            TimestampHelper.DurationFromMilliseconds(_consensusCommand.LimitMillisecondsOfMiningBlock),
            _consensusCommand.MiningDueTime);
        _consensusScheduler.CancelCurrentEvent();
        _consensusScheduler.NewEvent(leftMilliseconds.Milliseconds(), blockMiningEventData);
        
        stopwatch.Stop();
        _TriggerConsensusAsync.Record(stopwatch.ElapsedMilliseconds);

        Logger.LogDebug($"Set next mining time to: {_nextMiningTime.ToDateTime():hh:mm:ss.ffffff}");
    }

    /// <summary>
    ///     Call ACS4 method ValidateConsensusBeforeExecution.
    /// </summary>
    /// <param name="chainContext"></param>
    /// <param name="consensusExtraData"></param>
    /// <returns></returns>
    [Ump]
    public async Task<bool> ValidateConsensusBeforeExecutionAsync(ChainContext chainContext,
        byte[] consensusExtraData)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        stopwatch.Start();
        using var activity = _activitySource.StartActivity();

        var now = TimestampHelper.GetUtcNow();
        _blockTimeProvider.SetBlockTime(now, chainContext.BlockHash);

        var contractReaderContext =
            await _consensusReaderContextService.GetContractReaderContextAsync(chainContext);
        var validationResult = await _contractReaderFactory
            .Create(contractReaderContext)
            .ValidateConsensusBeforeExecution
            .CallAsync(new BytesValue { Value = ByteString.CopyFrom(consensusExtraData) });

        if (validationResult == null)
        {
            Logger.LogDebug("Validation of consensus failed before execution.");
            return false;
        }

        if (!validationResult.Success)
        {
            Logger.LogDebug($"Consensus validating before execution failed: {validationResult.Message}");
            await LocalEventBus.PublishAsync(new ConsensusValidationFailedEventData
            {
                ValidationResultMessage = validationResult.Message,
                IsReTrigger = validationResult.IsReTrigger
            });
        }
        
        stopwatch.Stop();
        _ValidateConsensusBeforeExecutionAsync.Record(stopwatch.ElapsedMilliseconds);

        return validationResult.Success;
    }

    /// <summary>
    ///     Call ACS4 method ValidateConsensusAfterExecution.
    /// </summary>
    /// <param name="chainContext"></param>
    /// <param name="consensusExtraData"></param>
    /// <returns></returns>
    [Ump]
    public async Task<bool> ValidateConsensusAfterExecutionAsync(ChainContext chainContext,
        byte[] consensusExtraData)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        stopwatch.Start();
        using var activity = _activitySource.StartActivity();

        var now = TimestampHelper.GetUtcNow();
        _blockTimeProvider.SetBlockTime(now, chainContext.BlockHash);

        var contractReaderContext =
            await _consensusReaderContextService.GetContractReaderContextAsync(chainContext);
        var validationResult = await _contractReaderFactory
            .Create(contractReaderContext)
            .ValidateConsensusAfterExecution
            .CallAsync(new BytesValue { Value = ByteString.CopyFrom(consensusExtraData) });

        if (validationResult == null)
        {
            Logger.LogDebug("Validation of consensus failed after execution.");
            return false;
        }

        if (!validationResult.Success)
        {
            Logger.LogDebug($"Consensus validating after execution failed: {validationResult.Message}");
            await LocalEventBus.PublishAsync(new ConsensusValidationFailedEventData
            {
                ValidationResultMessage = validationResult.Message,
                IsReTrigger = validationResult.IsReTrigger
            });
        }
        
        stopwatch.Stop();
        _ValidateConsensusAfterExecutionAsync.Record(stopwatch.ElapsedMilliseconds);

        return validationResult.Success;
    }

    /// <inheritdoc />
    /// <summary>
    ///     Get consensus block header extra data.
    /// </summary>
    /// <param name="chainContext"></param>
    /// <returns></returns>
    [Ump]
    public async Task<byte[]> GetConsensusExtraDataAsync(ChainContext chainContext)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        stopwatch.Start();
        using var activity = _activitySource.StartActivity();

        _blockTimeProvider.SetBlockTime(_nextMiningTime, chainContext.BlockHash);

        Logger.LogDebug(
            $"Block time of getting consensus extra data: {_nextMiningTime.ToDateTime():hh:mm:ss.ffffff}.");

        var contractReaderContext =
            await _consensusReaderContextService.GetContractReaderContextAsync(chainContext);
        var input = _triggerInformationProvider.GetTriggerInformationForBlockHeaderExtraData(
            _consensusCommand.ToBytesValue());
        var consensusContractStub = _contractReaderFactory.Create(contractReaderContext);
        var output = await consensusContractStub.GetConsensusExtraData.CallAsync(input);
        
        stopwatch.Stop();
        _GetConsensusExtraDataAsync.Record(stopwatch.ElapsedMilliseconds);
        
        return output.Value.ToByteArray();
    }

    /// <summary>
    ///     Get consensus system tx list.
    /// </summary>
    /// <param name="chainContext"></param>
    /// <returns></returns>
    [Ump]
    public async Task<List<Transaction>> GenerateConsensusTransactionsAsync(ChainContext chainContext)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        stopwatch.Start();
        using var activity = _activitySource.StartActivity();

        _blockTimeProvider.SetBlockTime(_nextMiningTime, chainContext.BlockHash);

        Logger.LogDebug(
            $"Block time of getting consensus system txs: {_nextMiningTime.ToDateTime():hh:mm:ss.ffffff}.");

        var contractReaderContext =
            await _consensusReaderContextService.GetContractReaderContextAsync(chainContext);
        var generatedTransactions =
            (await _contractReaderFactory
                .Create(contractReaderContext)
                .GenerateConsensusTransactions
                .CallAsync(_triggerInformationProvider.GetTriggerInformationForConsensusTransactions(
                    chainContext, _consensusCommand.ToBytesValue())))
            .Transactions
            .ToList();

        // Complete these transactions.
        foreach (var generatedTransaction in generatedTransactions)
        {
            generatedTransaction.RefBlockNumber = chainContext.BlockHeight;
            generatedTransaction.RefBlockPrefix =
                BlockHelper.GetRefBlockPrefix(chainContext.BlockHash);
            Logger.LogDebug($"Consensus transaction generated: \n{generatedTransaction.GetHash()}");
        }

        stopwatch.Stop();
        _GenerateConsensusTransactionsAsync.Record(stopwatch.ElapsedMilliseconds);
        return generatedTransactions;
    }
}