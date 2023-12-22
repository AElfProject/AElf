using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContract.Orleans;

public class SiloTransactionExecutingService : IPlainTransactionExecutingService
{

    private readonly ISiloClusterClientContext _siloClusterClientContext;
    private readonly ILogger<SiloTransactionExecutingService> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IConfiguration _configuration;
    private readonly int _defaultSiloInstanceCount = 2;
    private readonly int _grainActivation = 100;
    
    public SiloTransactionExecutingService(ISiloClusterClientContext siloClusterClientContext, ILogger<SiloTransactionExecutingService> logger, IClusterClient clusterClient, IConfiguration configuration)
    {
        _logger = logger;
        _siloClusterClientContext = siloClusterClientContext;
        _clusterClient = clusterClient;
        _configuration = configuration;
    }

    

    public ILocalEventBus LocalEventBus { get; set; }

    public async Task<List<ExecutionReturnSet>> ExecuteAsync(TransactionExecutingDto transactionExecutingDto,
        CancellationToken cancellationToken)
    {
        try
        {
            var siloInstanceCount = _configuration.GetValue("SiloInstanceCount", _defaultSiloInstanceCount);
            string id = "PlainTransactionExecutingService" + transactionExecutingDto.BlockHeader.Height % (siloInstanceCount * _grainActivation);
            var grain = _siloClusterClientContext.GetClusterClient().GetGrain<IPlainTransactionExecutingGrain>(id);
            var result = await grain.ExecuteAsync(transactionExecutingDto, cancellationToken);
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SiloTransactionExecutingService.ExecuteAsync: Failed while executing txs in block");
            throw;
        }
    }
}