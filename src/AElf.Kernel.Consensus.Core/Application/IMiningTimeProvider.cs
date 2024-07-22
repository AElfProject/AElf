using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.Application;

public interface IMiningTimeProvider
{
    Task SetLimitMillisecondsOfMiningBlockAsync(IBlockIndex blockIndex, long limit);
    Task<long> GetLimitMillisecondsOfMiningBlockAsync(IBlockIndex blockIndex);
}

public class MiningTimeProvider : BlockExecutedDataBaseProvider<Int64Value>,
    IMiningTimeProvider, ISingletonDependency
{
    private const string BlockExecutedDataName = "LimitMillisecondsOfMiningBlock";

    public MiningTimeProvider(
        ICachedBlockchainExecutedDataService<Int64Value> cachedBlockchainExecutedDataService) : base(
        cachedBlockchainExecutedDataService)
    {
        Logger = NullLogger<MiningTimeProvider>.Instance;
    }

    public ILogger<MiningTimeProvider> Logger { get; set; }

    public Task<long> GetLimitMillisecondsOfMiningBlockAsync(IBlockIndex blockIndex)
    {
        var limit = GetBlockExecutedData(blockIndex);
        return Task.FromResult(limit?.Value ?? 0);
    }

    public async Task SetLimitMillisecondsOfMiningBlockAsync(IBlockIndex blockIndex, long limit)
    {
        var blockTransactionLimit = new Int64Value
        {
            Value = limit
        };
        await AddBlockExecutedDataAsync(blockIndex, blockTransactionLimit);
        Logger.LogDebug($"LimitMillisecondsOfMiningBlock has been changed to {limit}");
    }

    protected override string GetBlockExecutedDataName()
    {
        return BlockExecutedDataName;
    }
}