using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContract.Orleans;

public class SiloTransactionExecutingService : IPlainTransactionExecutingService
{

    private readonly ISiloClusterClientContext _siloClusterClientContext;
    private readonly ILogger<SiloTransactionExecutingService> _logger;
    private readonly IPlainTransactionExecutingGrainProvider _plainTransactionExecutingGrainProvider;
    
    public SiloTransactionExecutingService(ISiloClusterClientContext siloClusterClientContext, ILogger<SiloTransactionExecutingService> logger, IClusterClient clusterClient, IConfiguration configuration,
        IPlainTransactionExecutingGrainProvider plainTransactionExecutingGrainProvider)
    {
        _logger = logger;
        _siloClusterClientContext = siloClusterClientContext;
        _plainTransactionExecutingGrainProvider = plainTransactionExecutingGrainProvider;
    }

    public ILocalEventBus LocalEventBus { get; set; }

    public async Task<List<ExecutionReturnSet>> ExecuteAsync(TransactionExecutingDto transactionExecutingDto,
        CancellationToken cancellationToken)
    {
        try
        {
            var id = _plainTransactionExecutingGrainProvider.TryGetGrainId(typeof(SiloTransactionExecutingService).Name, 
                out var pool);
            var grain = _siloClusterClientContext.GetClusterClient().GetGrain<IPlainTransactionExecutingGrain>(typeof(SiloTransactionExecutingService).Name + id);
            var result = await grain.ExecuteAsync(transactionExecutingDto, cancellationToken);
            pool.Add(id);
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SiloTransactionExecutingService.ExecuteAsync: Failed while executing txs in block");
            throw;
        }
    }
}