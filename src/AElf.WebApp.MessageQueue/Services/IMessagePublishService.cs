using System;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.WebApp.MessageQueue.Provider;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<MessagePublishService> _logger;
    private const string Asynchronous = "Asynchronous";
    private const string Synchronous = "Synchronous";

    public MessagePublishService(IDistributedEventBus distributedEventBus,
        IBlockMessageEtoProvider blockMessageEtoProvider, ILogger<MessagePublishService> logger)
    {
        _distributedEventBus = distributedEventBus;
        _blockMessageEtoProvider = blockMessageEtoProvider;
        _logger = logger;
    }

    public async Task<bool> PublishAsync(long height, CancellationToken cts)
    {
        var blockMessageEto = await _blockMessageEtoProvider.GetBlockMessageEtoByHeightAsync(height, cts);
        if (blockMessageEto != null) return await PublishAsync(blockMessageEto, Asynchronous);
        _logger.LogWarning($"Failed to find block information, height: {height}");
        return false;
    }

    public async Task<bool> PublishAsync(BlockExecutedSet blockExecutedSet)
    {
        var blockMessageEto = _blockMessageEtoProvider.GetBlockMessageEto(blockExecutedSet);
        return await PublishAsync(blockMessageEto, Synchronous);
    }

    private async Task<bool> PublishAsync(BlockMessageEto message, string runningPattern)
    {
        var height = message.Height;
        try
        {
            _logger.LogInformation($"{runningPattern} start publish block: {height} events.");
            await _distributedEventBus.PublishAsync(message);
            _logger.LogInformation($"{runningPattern} End publish block: {height} events.");
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to publish events to mq service.\n{e.Message}");
            return false;
        }
    }
}