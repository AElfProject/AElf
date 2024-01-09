using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Logging;
using Orleans;

namespace AElf.Kernel.SmartContract.Orleans;

public class PlainTransactionCleanChainGrain : Grain, IPlainTransactionCleanChainGrain
{
    private readonly ILogger<PlainTransactionCleanChainGrain> _logger;
    private readonly ISmartContractExecutiveService _smartContractExecutiveService;
    private readonly IBlockStateSetCachedStateStore _blockStateSetCachedStateStore;


    public PlainTransactionCleanChainGrain(
        ISmartContractExecutiveService smartContractExecutiveService,
        ILogger<PlainTransactionCleanChainGrain> logger,
        IBlockStateSetCachedStateStore blockStateSetCachedStateStore)
    {
        _smartContractExecutiveService = smartContractExecutiveService;
        _logger = logger;
        _blockStateSetCachedStateStore = blockStateSetCachedStateStore;
    }
    
    public async Task CleanChainStateAsync(long blockStateHeight)
    {
        _logger.LogDebug("PlainTransactionCleanChainGrain.CleanChainStateAsync,eventData:{blockStateHeight}",blockStateHeight);
        await _blockStateSetCachedStateStore.RemoveCache(blockStateHeight);
        _smartContractExecutiveService.CleanIdleExecutive();
    }
}