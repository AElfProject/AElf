using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.WebApp.MessageQueue.Provider;
using Volo.Abp.DependencyInjection;

namespace AElf.WebApp.MessageQueue.Services;

public interface IBlockMessageService
{
    Task<bool> SendMessageAsync(long height, CancellationToken cts = default);
    Task<bool> SendMessageAsync(BlockExecutedSet blockExecutedSet);
}

public class BlockMessageService : IBlockMessageService, ITransientDependency
{
    private readonly IMessagePublishService _messagePublishService;
    private readonly ISyncBlockStateProvider _syncBlockStateProvider;

    public BlockMessageService(IMessagePublishService messagePublishService,
        ISyncBlockStateProvider syncBlockStateProvider)
    {
        _messagePublishService = messagePublishService;
        _syncBlockStateProvider = syncBlockStateProvider;
    }


    public async Task<bool> SendMessageAsync(long height, CancellationToken cts)
    {
        var isSuccess = await _messagePublishService.PublishAsync(height, cts);
        if (!isSuccess)
            return false;
        await _syncBlockStateProvider.UpdateStateAsync(height + 1);
        return true;
    }

    public async Task<bool> SendMessageAsync(BlockExecutedSet blockExecutedSet)
    {
        var isSuccess = await _messagePublishService.PublishAsync(blockExecutedSet);
        if (!isSuccess)
            return false;
        await _syncBlockStateProvider.UpdateStateAsync(blockExecutedSet.Height);
        return true;
    }
}