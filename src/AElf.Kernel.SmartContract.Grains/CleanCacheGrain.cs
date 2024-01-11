using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.SmartContract.Grains;

public class CleanCacheGrain : Orleans.Grain, ICleanCacheGrain
{
    private readonly ILogger<CleanCacheGrain> _logger;
    private readonly ISmartContractExecutiveService _smartContractExecutiveService;
    private readonly IBlockStateSetCachedStateStore _blockStateSetCachedStateStore;

    public CleanCacheGrain(
        ISmartContractExecutiveService smartContractExecutiveService,
        ILogger<CleanCacheGrain> logger,
        IBlockStateSetCachedStateStore blockStateSetCachedStateStore)
    {
        _smartContractExecutiveService = smartContractExecutiveService;
        _logger = logger;
        _blockStateSetCachedStateStore = blockStateSetCachedStateStore;
    }
    
    public async Task CleanCacheAsync(long blockHeight)
    {
        _logger.LogInformation("CleanChainGrain.CleanCacheAsync,eventData:{blockHeight}",blockHeight);
        await _blockStateSetCachedStateStore.RemoveCacheAsync(blockHeight);
        _smartContractExecutiveService.CleanIdleExecutive();
    }
}