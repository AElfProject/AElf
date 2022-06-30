using System;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.WebApp.MessageQueue.Enum;
using AElf.WebApp.MessageQueue.Provider;
using AElf.WebApp.MessageQueue.Services;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.WebApp.MessageQueue;

public class BlockAcceptedEventHandler : ILocalEventHandler<BlockAcceptedEvent>, ITransientDependency
{
    private readonly IBlockMessageService _blockMessageService;
    private readonly ISyncBlockStateProvider _syncBlockStateProvider;
    private readonly ISendMessageByDesignateHeightTaskManager _sendMessageByDesignateHeightTaskManager;
    private readonly ILogger<BlockAcceptedEventHandler> _logger;

    public BlockAcceptedEventHandler(
        ISyncBlockStateProvider syncBlockStateProvider,
        ISendMessageByDesignateHeightTaskManager sendMessageByDesignateHeightTaskManager,
        IBlockMessageService blockMessageService, ILogger<BlockAcceptedEventHandler> logger)
    {
        _syncBlockStateProvider = syncBlockStateProvider;
        _sendMessageByDesignateHeightTaskManager = sendMessageByDesignateHeightTaskManager;
        _blockMessageService = blockMessageService;
        _logger = logger;
    }

    public async Task HandleEventAsync(BlockAcceptedEvent eventData)
    {
        var blockSyncState = await _syncBlockStateProvider.GetCurrentStateAsync();
        switch (blockSyncState.State)
        {
            case SyncState.Stopped:
                await StopAsync();
                return;
            case SyncState.SyncPrepared:
                await AsyncPreparedToRun(eventData);
                return;
            case SyncState.SyncRunning:
                await RunningAsync(eventData);
                return;
            case SyncState.Prepared:
                await PreparedToRunAsync(eventData);
                return;
            case SyncState.AsyncRunning:
                return;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task StopAsync()
    {
        _logger.LogInformation("Publish message has stopped");
        await _sendMessageByDesignateHeightTaskManager.StopAsync();
    }

    private async Task RunningAsync(BlockAcceptedEvent eventData)
    {
        _logger.LogInformation("Publish message synchronously");
        await _blockMessageService.SendMessageAsync(eventData.BlockExecutedSet);
    }

    private async Task PreparedToRunAsync(BlockAcceptedEvent eventData)
    {
        await _sendMessageByDesignateHeightTaskManager.StopAsync();
        var blockSyncState = await _syncBlockStateProvider.GetCurrentStateAsync();
        if (blockSyncState.CurrentHeight + 1 == eventData.Block.Height)
        {
            _logger.LogInformation("Publish message synchronously");
            await _blockMessageService.SendMessageAsync(eventData.BlockExecutedSet);
            await _syncBlockStateProvider.UpdateStateAsync(null, SyncState.SyncRunning);
        }

        else if (blockSyncState.CurrentHeight < eventData.Block.Height - 1)
        {
            await _syncBlockStateProvider.UpdateStateAsync(null, SyncState.AsyncRunning);
            _logger.LogInformation("Start to publish message asynchronously");
            var from = blockSyncState.CurrentHeight;
            _sendMessageByDesignateHeightTaskManager.Start(from);
        }
    }

    private async Task AsyncPreparedToRun(BlockAcceptedEvent eventData)
    {
        await _sendMessageByDesignateHeightTaskManager.StopAsync();
        var currentHeight = eventData.Block.Height;
        var blockSyncState = await _syncBlockStateProvider.GetCurrentStateAsync();
        if (blockSyncState.CurrentHeight >= currentHeight)
        {
            return;
        }

        var from = blockSyncState.CurrentHeight;
        var to = currentHeight - 2;
        if (from > to + 1 || to - from > 10)
        {
            await _syncBlockStateProvider.UpdateStateAsync(null, SyncState.Prepared);
            return;
        }

        await _syncBlockStateProvider.UpdateStateAsync(null, SyncState.SyncRunning);
        if (from >= to)
        {
            _logger.LogInformation($"Catch up to current block, from: {from + 1} - to: {to + 1}");
        }

        for (var i = from; i <= to; i++)
        {
            await _blockMessageService.SendMessageAsync(i);
        }

        await _blockMessageService.SendMessageAsync(eventData.BlockExecutedSet);
    }
}