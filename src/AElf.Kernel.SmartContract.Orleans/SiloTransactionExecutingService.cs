using System.Diagnostics;
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
    private readonly ActivitySource _activitySource;

    public SiloTransactionExecutingService(ISiloClusterClientContext siloClusterClientContext, ILogger<SiloTransactionExecutingService> logger, IClusterClient clusterClient, IConfiguration configuration,
        IPlainTransactionExecutingGrainProvider plainTransactionExecutingGrainProvider,
        Instrumentation instrumentation)
    {
        _logger = logger;
        _siloClusterClientContext = siloClusterClientContext;
        _plainTransactionExecutingGrainProvider = plainTransactionExecutingGrainProvider;
        _activitySource = instrumentation.ActivitySource;
    }

    public ILocalEventBus LocalEventBus { get; set; }

    public async Task<List<ExecutionReturnSet>> ExecuteAsync(TransactionExecutingDto transactionExecutingDto,
        CancellationToken cancellationToken)
    {
        try
        {
            var grain = _plainTransactionExecutingGrainProvider.GetGrain();
            var result = await grain.ExecuteAsync(transactionExecutingDto, cancellationToken);
            _plainTransactionExecutingGrainProvider.PutGrain(grain);
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed while executing txs in block");
            throw;
        }
    }
}