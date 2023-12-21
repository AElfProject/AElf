using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace AElf.Kernel.SmartContract.Orleans;

public class PlainTransactionCacheClearGrain : Grain, IPlainTransactionCacheClearGrain
{
    public ILogger<PlainTransactionCacheClearGrain> Logger { get; set; }
    private readonly ISmartContractExecutiveService _smartContractExecutiveService;
    private readonly ITaskQueueManager _taskQueueManager;
    private readonly IBlockchainStateService _blockchainStateService;


    public PlainTransactionCacheClearGrain(ITaskQueueManager taskQueueManager,
        IBlockchainStateService blockchainStateService,
        ISmartContractExecutiveService smartContractExecutiveService,
        ILogger<PlainTransactionCacheClearGrain> logger)
    {
        _taskQueueManager = taskQueueManager;
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