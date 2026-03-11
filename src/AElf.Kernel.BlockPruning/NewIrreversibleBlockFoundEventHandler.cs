using System.Threading.Tasks;
using AElf.Kernel.BlockPruning.Application;
using AElf.Kernel.Blockchain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.BlockPruning;

public class NewIrreversibleBlockFoundEventHandler : ILocalEventHandler<NewIrreversibleBlockFoundEvent>,
    ITransientDependency
{
    private readonly IBlockPruningService _blockPruningService;
    private readonly BlockPruningOptions _options;
    private readonly ITaskQueueManager _taskQueueManager;

    public ILogger<NewIrreversibleBlockFoundEventHandler> Logger { get; set; }

    public NewIrreversibleBlockFoundEventHandler(ITaskQueueManager taskQueueManager,
        IBlockPruningService blockPruningService,
        IOptionsSnapshot<BlockPruningOptions> options)
    {
        _taskQueueManager = taskQueueManager;
        _blockPruningService = blockPruningService;
        _options = options.Value;

        Logger = NullLogger<NewIrreversibleBlockFoundEventHandler>.Instance;
    }

    public Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
    {
        if (!_options.Enabled)
            return Task.CompletedTask;

        var queue = _taskQueueManager.GetQueue(BlockPruningConstants.BlockPruningQueueName);
        if (queue == null || queue.Size > 0)
        {
            Logger.LogDebug("Block pruning skipped: queue is busy (size={QueueSize})",
                queue?.Size ?? -1);
            return Task.CompletedTask;
        }

        Logger.LogDebug(
            "Enqueueing block pruning task (LIB height={LIBHeight})",
            eventData.BlockHeight);

        queue.Enqueue(async () => { await _blockPruningService.PruneBlockchainDataAsync(); });
        return Task.CompletedTask;
    }
}
