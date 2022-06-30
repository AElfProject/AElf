using System.Threading;
using System.Threading.Tasks;
using AElf.WebApp.MessageQueue.Enum;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.WebApp.MessageQueue.Provider;

public interface ISyncBlockStateProvider
{
    Task InitializeAsync();
    Task<SyncInformation> GetCurrentStateAsync();

    Task UpdateStateAsync(long? height, SyncState? state = null, SyncState? expectationState = null);
}

public class SyncBlockStateProvider : ISyncBlockStateProvider, ISingletonDependency
{
    private SyncInformation _blockSynStateInformation;
    private const string BlockSynState = "BlockSynState";
    private readonly IDistributedCache<SyncInformation> _distributedCache;
    private readonly MessageQueueOptions _messageQueueOptions;
    private readonly ILogger<SyncBlockStateProvider> _logger;
    private SemaphoreSlim SyncSemaphore { get; }

    public SyncBlockStateProvider(IDistributedCache<SyncInformation> distributedCache,
        IOptionsSnapshot<MessageQueueOptions> messageQueueEnableOptions, ILogger<SyncBlockStateProvider> logger)
    {
        _messageQueueOptions = messageQueueEnableOptions.Value;
        _distributedCache = distributedCache;
        _logger = logger;
        SyncSemaphore = new SemaphoreSlim(1, 1);
    }

    public async Task InitializeAsync()
    {
        _blockSynStateInformation = await _distributedCache.GetAsync(BlockSynState);
        if (_blockSynStateInformation != null)
        {
            _blockSynStateInformation.State = !_messageQueueOptions.Enable ? SyncState.Stopped : SyncState.Prepared;
        }
        else
        {
            _blockSynStateInformation = new SyncInformation
            {
                State = SyncState.Stopped,
                CurrentHeight = _messageQueueOptions.StartPublishMessageHeight > 1
                    ? _messageQueueOptions.StartPublishMessageHeight - 1
                    : 1
            };
        }

        _logger.LogInformation(
            $"BlockSynState initialized, State: {_blockSynStateInformation.State}  CurrentHeight: {_blockSynStateInformation.CurrentHeight}");
    }

    public async Task<SyncInformation> GetCurrentStateAsync()
    {
        var currentData = new SyncInformation();
        using (await SyncSemaphore.LockAsync())
        {
            currentData.State = _blockSynStateInformation.State;
            currentData.CurrentHeight = _blockSynStateInformation.CurrentHeight;
        }

        return currentData;
    }

    public async Task UpdateStateAsync(long? height, SyncState? state = null, SyncState? expectationState = null)
    {
        var dataToUpdate = new SyncInformation();
        using (await SyncSemaphore.LockAsync())
        {
            if (expectationState.HasValue && expectationState != _blockSynStateInformation.State)
            {
                return;
            }

            _blockSynStateInformation.State = state ?? _blockSynStateInformation.State;
            _blockSynStateInformation.CurrentHeight = height ?? _blockSynStateInformation.CurrentHeight;
            dataToUpdate.State = _blockSynStateInformation.State;
            dataToUpdate.CurrentHeight = _blockSynStateInformation.CurrentHeight;
            await _distributedCache.SetAsync(BlockSynState, dataToUpdate);
        }
        _logger.LogInformation(
            $"BlockSynState updated, State: {dataToUpdate.State}  CurrentHeight: {dataToUpdate.CurrentHeight}");
    }
}

public class SyncInformation
{
    public long CurrentHeight { get; set; }
    public SyncState State { get; set; }
}