using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Logging;
using Orleans;

namespace AElf.Kernel.SmartContract.Orleans;

public class PlainTransactionCleanChainGrain : Grain, IPlainTransactionCleanChainGrain
{
    private readonly ILogger<PlainTransactionCleanChainGrain> _logger;
    private readonly ISmartContractExecutiveService _smartContractExecutiveService;
    private readonly IBlockchainStateService _blockchainStateService;


    public PlainTransactionCleanChainGrain(
        IBlockchainStateService blockchainStateService,
        ISmartContractExecutiveService smartContractExecutiveService,
        ILogger<PlainTransactionCleanChainGrain> logger)
    {
        _blockchainStateService = blockchainStateService;
        _smartContractExecutiveService = smartContractExecutiveService;
        _logger = logger;
    }
    
    public async Task CleanChainStateAsync(long blockStateHeight)
    {
        _logger.LogDebug("PlainTransactionCleanChainGrain.CleanChainStateAsync,eventData:{blockStateHeight}",blockStateHeight);
        await _blockchainStateService.RemoveBlockStateSetsByHeightAsync(blockStateHeight);
        _smartContractExecutiveService.CleanIdleExecutive();
    }
}