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
        DateTime startTime = default;
        DateTime endTime= default;;
        try
        {
            startTime = DateTime.UtcNow;
            var grain = _plainTransactionExecutingGrainProvider.GetGrain();
            var result = await grain.ExecuteAsync(transactionExecutingDto, cancellationToken);
            _plainTransactionExecutingGrainProvider.PutGrain(grain);
            endTime = DateTime.UtcNow;
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SiloTransactionExecutingService.ExecuteAsync: Failed while executing txs in block");
            throw;
        }
        finally
        {
            _logger.LogDebug("BP ExecuteAsync finished usetime:{}", (endTime- startTime).TotalMilliseconds);
        }
    }
}