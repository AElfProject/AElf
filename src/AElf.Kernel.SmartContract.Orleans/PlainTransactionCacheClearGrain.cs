using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace AElf.Kernel.SmartContract.Orleans;

public class PlainTransactionCacheClearGrain : Grain, IPlainTransactionCacheClearGrain
{
    public ILogger<PlainTransactionCacheClearGrain> Logger { get; set; }
    private readonly ISmartContractExecutiveService _smartContractExecutiveService;
    private readonly IBlockchainStateService _blockchainStateService;


    public PlainTransactionCacheClearGrain(
        IBlockchainStateService blockchainStateService,
        ISmartContractExecutiveService smartContractExecutiveService,
        ILogger<PlainTransactionCacheClearGrain> logger)
    {
        _blockchainStateService = blockchainStateService;
        _smartContractExecutiveService = smartContractExecutiveService;
        Logger = logger;
    }
    
    public async Task CleanChainAsync(long blockStateHeight)
    {
        Logger.Info($"PlainTransactionCacheClearGrain.CleanChainAsync,eventData:{blockStateHeight}");
        await _blockchainStateService.RemoveBlockStateSetsByHeightAsync(blockStateHeight);
        _smartContractExecutiveService.CleanIdleExecutive();
    }
}