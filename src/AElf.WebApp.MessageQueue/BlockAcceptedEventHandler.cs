using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.WebApp.MessageQueue.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.WebApp.MessageQueue;

public class BlockAcceptedEventHandler : ILocalEventHandler<BlockAcceptedEvent>, ITransientDependency
{
    private readonly IBlockchainService _blockchainService;
    private readonly MessageQueueOptions _messageQueueOptions;
    private readonly IMessagePublishService _messagePublishService;

    public BlockAcceptedEventHandler(
        IBlockchainService blockchainService,
        IOptionsSnapshot<MessageQueueOptions> messageQueueEnableOptions,
        IMessagePublishService messagePublishService)
    {
        _blockchainService = blockchainService;
        _messagePublishService = messagePublishService;
        Logger = NullLogger<BlockAcceptedEventHandler>.Instance;
        _messageQueueOptions = messageQueueEnableOptions.Value;
    }

    public ILogger<BlockAcceptedEventHandler> Logger { get; set; }

    public async Task HandleEventAsync(BlockAcceptedEvent eventData)
    {
        var chain = await _blockchainService.GetChainAsync();
        if (!_messageQueueOptions.Enable ||
            chain.BestChainHeight < _messageQueueOptions.StartPublishMessageHeight)
            return;

        await _messagePublishService.PublishAsync(eventData.BlockExecutedSet);
    }
}