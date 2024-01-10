using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.SmartContract.Grain;

public class CleanChainGrain : Orleans.Grain, ICleanChainGrain
{
    private readonly ILogger<CleanChainGrain> _logger;
    private readonly ISmartContractExecutiveService _smartContractExecutiveService;
    private readonly IBlockStateSetCachedStateStore _blockStateSetCachedStateStore;


    public CleanChainGrain(
        ISmartContractExecutiveService smartContractExecutiveService,
        ILogger<CleanChainGrain> logger,
        IBlockStateSetCachedStateStore blockStateSetCachedStateStore)
    {
        _smartContractExecutiveService = smartContractExecutiveService;
        _logger = logger;
        _blockStateSetCachedStateStore = blockStateSetCachedStateStore;
    }
    
    public async Task CleanCacheAsync(long blockHeight)
    {
        _logger.LogDebug("CleanChainGrain.CleanCacheAsync,eventData:{blockHeight}",blockHeight);
        await _blockStateSetCachedStateStore.RemoveCacheAsync(blockHeight);
        _smartContractExecutiveService.CleanIdleExecutive();
    }
}