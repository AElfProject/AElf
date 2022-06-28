using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.WebApp.MessageQueue.Enum;
using AElf.WebApp.MessageQueue.Provider;
using AElf.WebApp.MessageQueue.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.WebApp.MessageQueue;

public class BlockAcceptedEventHandler : ILocalEventHandler<BlockAcceptedEvent>, ITransientDependency
{
    private readonly IBlockMessageService _blockMessageService;
    private readonly ISyncBlockStateProvider _syncBlockStateProvider;
    private readonly ISendMessageByDesignateHeightTaskManager _sendMessageByDesignateHeightTaskManager;

    public BlockAcceptedEventHandler(
        ISyncBlockStateProvider syncBlockStateProvider,
        ISendMessageByDesignateHeightTaskManager sendMessageByDesignateHeightTaskManager,
        IBlockMessageService blockMessageService)
    {
        _syncBlockStateProvider = syncBlockStateProvider;
        _sendMessageByDesignateHeightTaskManager = sendMessageByDesignateHeightTaskManager;
        _blockMessageService = blockMessageService;
        Logger = NullLogger<BlockAcceptedEventHandler>.Instance;
    }

    public ILogger<BlockAcceptedEventHandler> Logger { get; set; }

    public async Task HandleEventAsync(BlockAcceptedEvent eventData)
    {
        var blockSyncState = _syncBlockStateProvider.GetCurrentState();
        if (blockSyncState.State == SyncState.Stopped)
        {
            await _sendMessageByDesignateHeightTaskManager.StopAsync();
            return;
        }
        
        if (blockSyncState.CurrentHeight + 1 == eventData.Block.Height)
        {
            await _sendMessageByDesignateHeightTaskManager.StopAsync();
            await _blockMessageService.SendMessageAsync(eventData.BlockExecutedSet);
            if (blockSyncState.State == SyncState.Prepared)
            {
                await _syncBlockStateProvider.UpdateStateAsync(null, SyncState.Running);
            }
        }

        else if (blockSyncState.CurrentHeight < eventData.Block.Height && blockSyncState.State == SyncState.Prepared)
        {
            await _sendMessageByDesignateHeightTaskManager.StopAsync();
            blockSyncState = _syncBlockStateProvider.GetCurrentState();
            _sendMessageByDesignateHeightTaskManager.Start(blockSyncState.CurrentHeight + 1);
            await _syncBlockStateProvider.UpdateStateAsync(null, SyncState.Running);
        }
    }
}