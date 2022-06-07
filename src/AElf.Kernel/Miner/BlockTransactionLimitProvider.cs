using System.Collections.Generic;
using System.Linq;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Miner;

public interface IBlockTransactionLimitProvider
{
    Task<int> GetLimitAsync(IBlockIndex blockIndex);
    Task SetLimitAsync(IBlockIndex blockIndex, int limit);
}

internal class BlockTransactionLimitProvider : BlockExecutedDataBaseProvider<Int32Value>,
    IBlockTransactionLimitProvider,
    ISingletonDependency
{
    private const string BlockExecutedDataName = "BlockTransactionLimit";
    private readonly int _systemTransactionCount;

    public BlockTransactionLimitProvider(
        ICachedBlockchainExecutedDataService<Int32Value> cachedBlockchainExecutedDataService,
        IEnumerable<ISystemTransactionGenerator> systemTransactionGenerators) : base(
        cachedBlockchainExecutedDataService)
    {
        _systemTransactionCount = systemTransactionGenerators.Count();
    }

    public ILogger<BlockTransactionLimitProvider> Logger { get; set; }

    public Task<int> GetLimitAsync(IBlockIndex blockIndex)
    {
        var limit = GetBlockExecutedData(blockIndex);
        return Task.FromResult(limit?.Value ?? int.MaxValue);
    }

    public async Task SetLimitAsync(IBlockIndex blockIndex, int limit)
    {
        if (limit <= _systemTransactionCount)
            return;

        var blockTransactionLimit = new Int32Value
        {
            Value = limit
        };
        await AddBlockExecutedDataAsync(blockIndex, blockTransactionLimit);
        Logger.LogDebug($"BlockTransactionLimit has been changed to {limit}");
    }

    protected override string GetBlockExecutedDataName()
    {
        return BlockExecutedDataName;
    }
}