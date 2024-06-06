using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForDelayedMethodFee;

internal interface ITotalDelayedTransactionFeesMapProvider
{
    Task<TotalDelayedTransactionFeesMap> GetTotalDelayedTransactionFeesMapAsync(IChainContext chainContext);
    Task SetTotalDelayedTransactionFeesMapAsync(IBlockIndex blockIndex, TotalDelayedTransactionFeesMap totalTransactionFeesMap);
}

internal class TotalDelayedTransactionFeesMapProvider : BlockExecutedDataBaseProvider<TotalDelayedTransactionFeesMap>,
    ITotalDelayedTransactionFeesMapProvider, ISingletonDependency
{
    private const string BlockExecutedDataName = nameof(TotalDelayedTransactionFeesMap);

    public TotalDelayedTransactionFeesMapProvider(
        ICachedBlockchainExecutedDataService<TotalDelayedTransactionFeesMap> cachedBlockchainExecutedDataService) :
        base(
            cachedBlockchainExecutedDataService)
    {
    }

    public Task<TotalDelayedTransactionFeesMap> GetTotalDelayedTransactionFeesMapAsync(IChainContext chainContext)
    {
        var totalTxFeesMap = GetBlockExecutedData(chainContext);
        return Task.FromResult(totalTxFeesMap);
    }

    public async Task SetTotalDelayedTransactionFeesMapAsync(IBlockIndex blockIndex,
        TotalDelayedTransactionFeesMap totalTransactionFeesMap)
    {
        await AddBlockExecutedDataAsync(blockIndex, totalTransactionFeesMap);
    }

    protected override string GetBlockExecutedDataName()
    {
        return BlockExecutedDataName;
    }
}