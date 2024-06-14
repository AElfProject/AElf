using System.Collections.Generic;
using System.Linq;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.TransactionPool;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;

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
    private TransactionOptions _txOptions;
    private const string BlockExecutedDataName = "BlockTransactionLimit";
    private readonly int _systemTransactionCount;

    public BlockTransactionLimitProvider(
        ICachedBlockchainExecutedDataService<Int32Value> cachedBlockchainExecutedDataService,
        IEnumerable<ISystemTransactionGenerator> systemTransactionGenerators,
        IOptionsMonitor<TransactionOptions> txOptions) : base(
        cachedBlockchainExecutedDataService)
    {
        _txOptions = txOptions.CurrentValue;
        txOptions.OnChange(newOptions =>
        {
            _txOptions = newOptions;
        });
        _systemTransactionCount = systemTransactionGenerators.Count();
    }

    public ILogger<BlockTransactionLimitProvider> Logger { get; set; }

    public Task<int> GetLimitAsync(IBlockIndex blockIndex)
    {
        var limit = _txOptions.BlockTransactionLimit;
        Logger.LogInformation($"BlockTransactionLimit is {limit}");
        return Task.FromResult(limit);
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