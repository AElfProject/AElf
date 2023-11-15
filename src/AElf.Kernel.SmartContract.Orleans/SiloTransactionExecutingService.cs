using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContract.Orleans;

public class SiloTransactionExecutingService : IPlainTransactionExecutingService, ISingletonDependency
{

    private readonly ISiloClusterClientContext _siloClusterClientContext;
    private readonly ILogger<SiloTransactionExecutingService> _logger;
    private readonly IClusterClient _clusterClient;


    public SiloTransactionExecutingService(ISiloClusterClientContext siloClusterClientContext, ILogger<SiloTransactionExecutingService> logger, IClusterClient clusterClient)
    {
        _logger = logger;
        _siloClusterClientContext = siloClusterClientContext;
        _clusterClient = clusterClient;
    }

    public ILogger<PlainTransactionExecutingService> Logger { get; set; }

    public ILocalEventBus LocalEventBus { get; set; }

    public async Task<List<ExecutionReturnSet>> ExecuteAsync(TransactionExecutingDto transactionExecutingDto,
        CancellationToken cancellationToken)
    {
        try
        {
            var grain = _siloClusterClientContext.GetClusterClient().GetGrain<IPlainTransactionExecutingGrain>(Guid.NewGuid());
            var result = await grain.ExecuteAsync(transactionExecutingDto, cancellationToken);
            return result;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "SiloTransactionExecutingService.ExecuteAsync: Failed while executing txs in block");
            throw;
        }
    }
}