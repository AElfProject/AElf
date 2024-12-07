using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers;

public class BlockAcceptedEventHandler : ILocalEventHandler<BlockAcceptedEvent>, ITransientDependency
{
    private readonly INetworkService _networkService;
    private readonly ISyncStateService _syncStateService;
    private readonly IAccountService _accountService;
    private readonly ITaskQueueManager _taskQueueManager;

    public BlockAcceptedEventHandler(INetworkService networkService, ISyncStateService syncStateService,
        IAccountService accountService, ITaskQueueManager taskQueueManager)
    {
        _taskQueueManager = taskQueueManager;
        _networkService = networkService;
        _syncStateService = syncStateService;
        _accountService = accountService;
    }

    public Task HandleEventAsync(BlockAcceptedEvent eventData)
    {
        if (_syncStateService.SyncState == SyncState.Finished)
            // if sync is finished we announce the block
            _networkService.BroadcastAnnounceAsync(eventData.Block.Header);
        else if (_syncStateService.SyncState == SyncState.Syncing)
            // if syncing and the block is higher the current target, try and update.
            if (_syncStateService.GetCurrentSyncTarget() <= eventData.Block.Header.Height)
                _taskQueueManager.Enqueue(async () => { await _syncStateService.UpdateSyncStateAsync(); },
                    OSConstants.InitialSyncQueueName);

        _taskQueueManager.Enqueue(async () =>
        {
            var blockHash = eventData.Block.GetHash();
            var blockHeight = eventData.Block.Height;
            var blsSignature = await _accountService.BlsSignAsync(new BlockConfirmation
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight
            }.ToByteArray());
            await _networkService.BroadcastBlockConfirmationAsync(blockHash, blockHeight, blsSignature);
        }, OSConstants.BlockConfirmationQueueName);

        return Task.CompletedTask;
    }
}