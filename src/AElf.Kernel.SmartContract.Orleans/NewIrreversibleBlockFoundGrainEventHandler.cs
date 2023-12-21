using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContract.Orleans;

public class NewIrreversibleBlockFoundGrainEventHandler : ILocalEventHandler<NewIrreversibleBlockFoundEvent>,
    ITransientDependency
{
    private readonly IBlockchainStateService _blockchainStateService;
    private readonly ISmartContractExecutiveService _smartContractExecutiveService;
    private readonly ITaskQueueManager _taskQueueManager;
    private readonly int _defaultSiloInstanceCount = 2;
    private readonly int _grainActivation = 10;
    private readonly IConfiguration _configuration;
    private readonly ISiloClusterClientContext _siloClusterClientContext;

    public NewIrreversibleBlockFoundGrainEventHandler(ITaskQueueManager taskQueueManager,
        IBlockchainStateService blockchainStateService,
        ISmartContractExecutiveService smartContractExecutiveService,
        IConfiguration configuration,
        ISiloClusterClientContext siloClusterClientContext)
    {
        _taskQueueManager = taskQueueManager;
        _blockchainStateService = blockchainStateService;
        _smartContractExecutiveService = smartContractExecutiveService;
        _configuration = configuration;
        _siloClusterClientContext = siloClusterClientContext;
        Logger = NullLogger<NewIrreversibleBlockFoundGrainEventHandler>.Instance;
    }

    public ILogger<NewIrreversibleBlockFoundGrainEventHandler> Logger { get; set; }
    public ILocalEventBus LocalEventBus { get; set; }

    public Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
    {
        Logger.Info($"NewIrreversibleBlockFoundGrainEventHandler.HandleEventAsync,eventData:{JsonConvert.SerializeObject(eventData)}");

        _taskQueueManager.Enqueue(async () =>
        {
            var siloInstanceCount = _configuration.GetValue("SiloInstanceCount", _defaultSiloInstanceCount);
            string id = "PlainTransactionCacheClearGrain" + eventData.BlockHeight % (siloInstanceCount * _grainActivation);
            var grain = _siloClusterClientContext.GetClusterClient().GetGrain<IPlainTransactionCacheClearGrain>(id);
            await grain.CleanChainAsync(eventData.BlockHeight);
        }, KernelConstants.ChainCleaningQueueName);
        return Task.CompletedTask;
    }
}