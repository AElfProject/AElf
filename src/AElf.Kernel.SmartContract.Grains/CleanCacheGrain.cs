using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.SmartContract.Grains;

[CleanCache]
public class CleanCacheGrain : Orleans.Grain, ICleanCacheGrain
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