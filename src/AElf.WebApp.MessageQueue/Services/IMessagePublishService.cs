using System;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.WebApp.MessageQueue.Helpers;
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
    private readonly IBlockMessageEtoGenerator _blockMessageEtoGenerator;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly ILogger<MessagePublishService> _logger;
    private const string Asynchronous = "Asynchronous";
    private const string Synchronous = "Synchronous";

    public MessagePublishService(IDistributedEventBus distributedEventBus,
        IBlockMessageEtoGenerator blockMessageEtoGenerator, ILogger<MessagePublishService> logger)
    {
        _distributedEventBus = distributedEventBus;
        _blockMessageEtoGenerator = blockMessageEtoGenerator;
        _logger = logger;
    }

    public async Task<bool> PublishAsync(long height, CancellationToken cts)
    {
        var blockMessageEto = await _blockMessageEtoGenerator.GetBlockMessageEtoByHeightAsync(height, cts);
        if (blockMessageEto != null)
        {
            return await PublishAsync(blockMessageEto, Asynchronous);
        }

        return false;
    }

    public async Task<bool> PublishAsync(BlockExecutedSet blockExecutedSet)
    {
        var blockMessageEto = _blockMessageEtoGenerator.GetBlockMessageEto(blockExecutedSet);
        return await PublishAsync(blockMessageEto, Synchronous);
    }

    private async Task<bool> PublishAsync(IBlockMessage message, string runningPattern)
    {
        _logger.LogInformation($"{runningPattern} start publish block: {message.Height}.");
        try
        {
            await _distributedEventBus.PublishAsync(message.GetType(), message);
            _logger.LogInformation($"{runningPattern} End publish block: {message.Height}.");
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to publish events to mq service.\n{e.Message}");
            return false;
        }
    }
}