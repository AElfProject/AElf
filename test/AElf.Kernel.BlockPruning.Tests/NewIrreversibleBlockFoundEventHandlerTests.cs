using System.Diagnostics;
using AElf.Kernel.BlockPruning.Domain;
using AElf.Kernel.Blockchain.Events;

namespace AElf.Kernel.BlockPruning;

public sealed class EventHandlerEnabledTests : BlockPruningServiceTestBase
{
    private readonly IBlockPruningInfoManager _blockPruningInfoManager;
    private readonly ITaskQueueManager _taskQueueManager;
    private readonly NewIrreversibleBlockFoundEventHandler _eventHandler;

    public EventHandlerEnabledTests()
    {
        _taskQueueManager = GetRequiredService<ITaskQueueManager>();
        _blockPruningInfoManager = GetRequiredService<IBlockPruningInfoManager>();
        _eventHandler = GetRequiredService<NewIrreversibleBlockFoundEventHandler>();
    }

    [Fact]
    public async Task HandleEvent_ShouldEnqueueTask()
    {
        var evt = new NewIrreversibleBlockFoundEvent
        {
            BlockHeight = 10,
            BlockHash = HashHelper.ComputeFrom("block10")
        };

        await _eventHandler.HandleEventAsync(evt);

        var queue = _taskQueueManager.GetQueue(BlockPruningConstants.BlockPruningQueueName);
        queue.ShouldNotBeNull();
    }

    [Fact]
    public async Task HandleEvent_PruningExecuted_Test()
    {
        var evt = new NewIrreversibleBlockFoundEvent
        {
            BlockHeight = 10,
            BlockHash = HashHelper.ComputeFrom("block10")
        };

        await _eventHandler.HandleEventAsync(evt);

        var sw = Stopwatch.StartNew();
        long height = 0;
        while (sw.ElapsedMilliseconds < 5000)
        {
            height = await _blockPruningInfoManager.GetLastPrunedHeightAsync();
            if (height > 0) break;
            await Task.Delay(100);
        }

        height.ShouldBeGreaterThan(0);
    }
}

public sealed class EventHandlerDisabledTests : BlockPruningDisabledTestBase
{
    private readonly IBlockPruningInfoManager _blockPruningInfoManager;
    private readonly ITaskQueueManager _taskQueueManager;
    private readonly NewIrreversibleBlockFoundEventHandler _eventHandler;

    public EventHandlerDisabledTests()
    {
        _taskQueueManager = GetRequiredService<ITaskQueueManager>();
        _blockPruningInfoManager = GetRequiredService<IBlockPruningInfoManager>();
        _eventHandler = GetRequiredService<NewIrreversibleBlockFoundEventHandler>();
    }

    [Fact]
    public async Task HandleEvent_Disabled_ShouldNotEnqueue()
    {
        var evt = new NewIrreversibleBlockFoundEvent
        {
            BlockHeight = 10,
            BlockHash = HashHelper.ComputeFrom("block10")
        };

        await _eventHandler.HandleEventAsync(evt);

        await Task.Delay(500);

        var height = await _blockPruningInfoManager.GetLastPrunedHeightAsync();
        height.ShouldBe(0);
    }
}
