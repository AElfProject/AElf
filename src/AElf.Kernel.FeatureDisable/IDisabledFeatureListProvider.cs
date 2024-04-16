using AElf.Kernel.SmartContract.Application;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeatureDisable;

public interface IDisabledFeatureListProvider
{
    Task SetDisabledFeatureListAsync(IBlockIndex blockIndex, string disabledFeatures);
    Task<string[]> GetDisabledFeatureListAsync(IBlockIndex blockIndex);
}

public class DisabledFeatureListProvider : BlockExecutedDataBaseProvider<StringValue>,
    IDisabledFeatureListProvider, ISingletonDependency
{
    private const string BlockExecutedDataName = "DisabledFeatureList";

    public ILogger<DisabledFeatureListProvider> Logger { get; set; }

    public DisabledFeatureListProvider(
        ICachedBlockchainExecutedDataService<StringValue> cachedBlockchainExecutedDataService) : base(
        cachedBlockchainExecutedDataService)
    {
        Logger = NullLogger<DisabledFeatureListProvider>.Instance;
    }

    public async Task SetDisabledFeatureListAsync(IBlockIndex blockIndex, string disabledFeatures)
    {
        var blockTransactionLimit = new StringValue
        {
            Value = disabledFeatures
        };
        await AddBlockExecutedDataAsync(blockIndex, blockTransactionLimit);
        Logger.LogDebug($"DisabledFeatureList has been changed to {disabledFeatures}");
    }

    public Task<string[]> GetDisabledFeatureListAsync(IBlockIndex blockIndex)
    {
        var limit = GetBlockExecutedData(blockIndex);
        return Task.FromResult(limit?.Value.Split(',') ?? Array.Empty<string>());
    }

    protected override string GetBlockExecutedDataName()
    {
        return BlockExecutedDataName;
    }
}