using System.Threading.Tasks;
using AElf.WebApp.MessageQueue.Enum;
using Microsoft.Extensions.Options;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace AElf.WebApp.MessageQueue.Provider;

public interface ISyncBlockStateProvider
{
    Task InitializeAsync();
    SyncInformation GetCurrentState();

    Task UpdateStateAsync(long? height, SyncState? state = null);
}

public class SyncBlockStateProvider : ISyncBlockStateProvider, ISingletonDependency
{
    private SyncInformation _blockSynStateInformation;
    private const string BlockSynState = "BlockSynState";
    private readonly IDistributedCache<SyncInformation> _distributedCache;
    private readonly MessageQueueOptions _messageQueueOptions;

    public SyncBlockStateProvider(IDistributedCache<SyncInformation> distributedCache,
        IOptionsSnapshot<MessageQueueOptions> messageQueueEnableOptions)
    {
        _messageQueueOptions = messageQueueEnableOptions.Value;
        _distributedCache = distributedCache;
    }

    public async Task InitializeAsync()
    {
        _blockSynStateInformation = await _distributedCache.GetAsync(BlockSynState);
        _blockSynStateInformation.State = !_messageQueueOptions.Enable ? SyncState.Stopped : SyncState.Prepared;
    }

    public SyncInformation GetCurrentState()
    {
        var currentData = new SyncInformation();
        lock (_distributedCache)
        {
            currentData.State = _blockSynStateInformation.State;
            currentData.CurrentHeight = _blockSynStateInformation.CurrentHeight;
        }

        return currentData;
    }

    public async Task UpdateStateAsync(long? height, SyncState? state = null)
    {
        var dataToUpdate = new SyncInformation();
        lock (_distributedCache)
        {
            if (_blockSynStateInformation == null)
            {
                _blockSynStateInformation = new SyncInformation
                {
                    State = state ?? SyncState.Stopped,
                    CurrentHeight = height ?? 0
                };
            }
            else
            {
                _blockSynStateInformation.State = state ?? _blockSynStateInformation.State;
                _blockSynStateInformation.CurrentHeight = height ?? _blockSynStateInformation.CurrentHeight;
            }

            dataToUpdate.State = _blockSynStateInformation.State;
            dataToUpdate.CurrentHeight = _blockSynStateInformation.CurrentHeight;
        }

        await _distributedCache.SetAsync(BlockSynState, dataToUpdate);
    }
}

public class SyncInformation
{
    public long CurrentHeight { get; set; }
    public SyncState State { get; set; }
}