using System.Threading;
using System.Threading.Tasks;
using AElf.WebApp.MessageQueue.Provider;
using Volo.Abp.DependencyInjection;

namespace AElf.WebApp.MessageQueue.Services;

public interface IBlockMessageService
{
    Task<bool> SendMessageAsync(long height, CancellationToken cts);
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
        await _syncBlockStateProvider.UpdateAsync(height);
        return true;
    }
}