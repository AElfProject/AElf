using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.Grains;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
            //var client = _clusterClient.GetGrain<IPlainTransactionExecutingGrain>(Guid.NewGuid());
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