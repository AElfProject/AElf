using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Orleans.Strategy;
using Orleans;

namespace AElf.Kernel.SmartContract.Orleans;

[CleanCache]
public class CleanCacheGrain : Grain, ICleanCacheGrain
{
    private readonly ISmartContractExecutiveService _smartContractExecutiveService;
    private readonly IBlockStateSetCachedStateStore _blockStateSetCachedStateStore;

    public CleanCacheGrain(
        ISmartContractExecutiveService smartContractExecutiveService,
        IBlockStateSetCachedStateStore blockStateSetCachedStateStore)
    {
        _smartContractExecutiveService = smartContractExecutiveService;
        _blockStateSetCachedStateStore = blockStateSetCachedStateStore;
    }
    
    public async Task CleanCacheAsync(long blockHeight)
    {
        await _blockStateSetCachedStateStore.RemoveCacheAsync(blockHeight);
        _smartContractExecutiveService.CleanIdleExecutive();
    }
}