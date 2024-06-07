using AElf.Kernel.Blockchain;
using AElf.Kernel.SmartContract.Application;

namespace AElf.Kernel.FeatureDisable.Tests;

public class OptionalLogEventProcessingService<T> : ILogEventProcessingService<T> where T : ILogEventProcessor
{
    private readonly LogEventProcessingService<T> _inner;

    public OptionalLogEventProcessingService(LogEventProcessingService<T> inner)
    {
        _inner = inner;
    }

    public static bool Enabled { get; set; }

    public async Task ProcessAsync(List<BlockExecutedSet> blockExecutedSets)
    {
        if (Enabled) await _inner.ProcessAsync(blockExecutedSets);
    }
}