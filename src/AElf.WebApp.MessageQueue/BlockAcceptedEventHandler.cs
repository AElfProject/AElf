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
        var blockSyncState = _syncBlockStateProvider.GetCurrentState();
        if (blockSyncState.State == SyncState.Stopped)
        {
            _logger.LogInformation("Publish message has stopped");
            await _sendMessageByDesignateHeightTaskManager.StopAsync();
            return;
        }

        if (blockSyncState.CurrentHeight + 1 == eventData.Block.Height)
        {
            if (blockSyncState.State == SyncState.Prepared)
            {
                await _syncBlockStateProvider.UpdateStateAsync(null, SyncState.Running, SyncState.Prepared);
            }
            _logger.LogInformation("Publish message synchronously");
            await _sendMessageByDesignateHeightTaskManager.StopAsync();
            await _blockMessageService.SendMessageAsync(eventData.BlockExecutedSet);
        }

        else if (blockSyncState.CurrentHeight < eventData.Block.Height && blockSyncState.State == SyncState.Prepared)
        {
            _logger.LogInformation("Start to publish message asynchronously");
            await _sendMessageByDesignateHeightTaskManager.StopAsync();
            blockSyncState = _syncBlockStateProvider.GetCurrentState();
            await _syncBlockStateProvider.UpdateStateAsync(null, SyncState.Running, SyncState.Prepared);
            _sendMessageByDesignateHeightTaskManager.Start(blockSyncState.CurrentHeight + 1);
        }
    }
}