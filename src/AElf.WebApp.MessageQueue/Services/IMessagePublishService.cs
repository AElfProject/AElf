using System;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.WebApp.MessageQueue.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AElf.WebApp.MessageQueue.Services;

public interface IMessagePublishService
{
    Task<bool> PublishAsync(long height, CancellationToken cts);
    Task<bool> PublishAsync(BlockExecutedSet blockExecutedSet);
}

public class MessagePublishService : IMessagePublishService, ITransientDependency
{
    private readonly IBlockMessageEtoProvider _blockMessageEtoProvider;
    private readonly IDistributedEventBus _distributedEventBus;
    public ILogger<MessagePublishService> Logger { get;}

    public MessagePublishService(IDistributedEventBus distributedEventBus,
        IBlockMessageEtoProvider blockMessageEtoProvider)
    {
        _distributedEventBus = distributedEventBus;
        _blockMessageEtoProvider = blockMessageEtoProvider;
        Logger = NullLogger<MessagePublishService>.Instance;
    }

    public async Task<bool> PublishAsync(long height, CancellationToken cts)
    {
        var blockMessageEto = await _blockMessageEtoProvider.GetBlockMessageEtoByHeightAsync(height, cts);
        if (blockMessageEto != null) return await PublishAsync(blockMessageEto);
        Logger.LogWarning($"Failed to find block information, height: {height}");
        return false;

    }

    public async Task<bool> PublishAsync(BlockExecutedSet blockExecutedSet)
    {
        var blockMessageEto = _blockMessageEtoProvider.GetBlockMessageEto(blockExecutedSet);
        return await PublishAsync(blockMessageEto);
    }

    private async Task<bool> PublishAsync(BlockMessageEto message)
    {
        var height = message.Height;
        try
        {
            Logger.LogInformation($"Start publish block: {height} events.");
            await _distributedEventBus.PublishAsync(message);
            Logger.LogInformation($"End publish block: {height} events.");
            return true;
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to publish events to mq service.\n{e.Message}");
            return false;
        }
    }
}