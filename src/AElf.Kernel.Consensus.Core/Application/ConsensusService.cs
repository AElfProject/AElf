using System;
using System.Collections.Generic;
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
    private readonly IMiningTimeProvider _miningTimeProvider;
    private readonly IConsensusScheduler _consensusScheduler;

    private readonly IContractReaderFactory<ConsensusContractContainer.ConsensusContractStub>
        _contractReaderFactory;

    private readonly ITriggerInformationProvider _triggerInformationProvider;
    private ConsensusCommand _consensusCommand;

    private Timestamp _nextMiningTime;

    public ConsensusService(IConsensusScheduler consensusScheduler,
        IContractReaderFactory<ConsensusContractContainer.ConsensusContractStub> contractReaderFactory,
        ITriggerInformationProvider triggerInformationProvider,
        IBlockTimeProvider blockTimeProvider, IConsensusReaderContextService consensusReaderContextService,
        IMiningTimeProvider miningTimeProvider)
    {
        _contractReaderFactory = contractReaderFactory;
        _triggerInformationProvider = triggerInformationProvider;
        _blockTimeProvider = blockTimeProvider;
        _consensusReaderContextService = consensusReaderContextService;
        _consensusScheduler = consensusScheduler;
        _miningTimeProvider = miningTimeProvider;

        Logger = NullLogger<ConsensusService>.Instance;
        LocalEventBus = NullLocalEventBus.Instance;
    }

    public ILocalEventBus LocalEventBus { get; set; }

    public ILogger<ConsensusService> Logger { get; set; }

    /// <summary>
    ///     Basically update the consensus scheduler with latest consensus command.
    /// </summary>
    /// <param name="chainContext"></param>
    /// <returns></returns>
    public async Task TriggerConsensusAsync(ChainContext chainContext)
    {
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

        var configuredMiningTime = await _miningTimeProvider.GetLimitMillisecondsOfMiningBlockAsync(new BlockIndex
        {
            BlockHeight = chainContext.BlockHeight,
            BlockHash = chainContext.BlockHash
        });
        var limitMillisecondsOfMiningBlock = configuredMiningTime == 0
            ? _consensusCommand.LimitMillisecondsOfMiningBlock
            : configuredMiningTime;
        // Update consensus scheduler.
        var blockMiningEventData = new ConsensusRequestMiningEventData(chainContext.BlockHash,
            chainContext.BlockHeight,
            _nextMiningTime,
            TimestampHelper.DurationFromMilliseconds(limitMillisecondsOfMiningBlock),
            _consensusCommand.MiningDueTime);
        _consensusScheduler.CancelCurrentEvent();
        _consensusScheduler.NewEvent(leftMilliseconds.Milliseconds(), blockMiningEventData);

        Logger.LogDebug($"Set next mining time to: {_nextMiningTime.ToDateTime():hh:mm:ss.ffffff}");
    }

    /// <summary>
    ///     Call ACS4 method ValidateConsensusBeforeExecution.
    /// </summary>
    /// <param name="chainContext"></param>
    /// <param name="consensusExtraData"></param>
    /// <returns></returns>
    public async Task<bool> ValidateConsensusBeforeExecutionAsync(ChainContext chainContext,
        byte[] consensusExtraData)
    {
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

        return validationResult.Success;
    }

    /// <summary>
    ///     Call ACS4 method ValidateConsensusAfterExecution.
    /// </summary>
    /// <param name="chainContext"></param>
    /// <param name="consensusExtraData"></param>
    /// <returns></returns>
    public async Task<bool> ValidateConsensusAfterExecutionAsync(ChainContext chainContext,
        byte[] consensusExtraData)
    {
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

        return validationResult.Success;
    }

    /// <inheritdoc />
    /// <summary>
    ///     Get consensus block header extra data.
    /// </summary>
    /// <param name="chainContext"></param>
    /// <returns></returns>
    public async Task<byte[]> GetConsensusExtraDataAsync(ChainContext chainContext)
    {
        _blockTimeProvider.SetBlockTime(_nextMiningTime, chainContext.BlockHash);

        Logger.LogDebug(
            $"Block time of getting consensus extra data: {_nextMiningTime.ToDateTime():hh:mm:ss.ffffff}.");

        var contractReaderContext =
            await _consensusReaderContextService.GetContractReaderContextAsync(chainContext);
        var input = _triggerInformationProvider.GetTriggerInformationForBlockHeaderExtraData(
            _consensusCommand.ToBytesValue());
        var consensusContractStub = _contractReaderFactory.Create(contractReaderContext);
        var output = await consensusContractStub.GetConsensusExtraData.CallAsync(input);
        return output.Value.ToByteArray();
    }

    /// <summary>
    ///     Get consensus system tx list.
    /// </summary>
    /// <param name="chainContext"></param>
    /// <returns></returns>
    public async Task<List<Transaction>> GenerateConsensusTransactionsAsync(ChainContext chainContext)
    {
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

        return generatedTransactions;
    }
}