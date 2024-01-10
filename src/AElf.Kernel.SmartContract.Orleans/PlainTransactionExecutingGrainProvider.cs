using System.Collections.Concurrent;
using AElf.Kernel.SmartContract.Grains;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;

namespace AElf.Kernel.SmartContract.Orleans;

public interface IPlainTransactionExecutingGrainProvider
{
    IPlainTransactionExecutingGrain GetGrain(); 
    void PutGrain(IPlainTransactionExecutingGrain grain);
}

public class PlainTransactionExecutingGrainProvider : IPlainTransactionExecutingGrainProvider, ISingletonDependency
{
    private readonly ISiloClusterClientContext _siloClusterClientContext;
    private readonly ILogger<PlainTransactionExecutingGrainProvider> _logger;
    private readonly ConcurrentBag<Guid> _pool = new();
    private readonly IGuidGenerator _guidGenerator;
    public PlainTransactionExecutingGrainProvider(ILogger<PlainTransactionExecutingGrainProvider> logger,
        ISiloClusterClientContext siloClusterClientContext, IGuidGenerator guidGenerator)
    {
        _logger = logger;
        _siloClusterClientContext = siloClusterClientContext;
        _guidGenerator = guidGenerator;
    }
    public IPlainTransactionExecutingGrain GetGrain()
    {
        if (!_pool.TryTake(out var id))
        {
            id = _guidGenerator.Create();
            _logger.LogDebug("Create new Id for {id}",id);
        }
        var grain = _siloClusterClientContext.GetClusterClient().GetGrain<IPlainTransactionExecutingGrain>(id);
        return grain;
    }
    public void PutGrain(IPlainTransactionExecutingGrain grain)
    {
        _pool.Add(grain.GetPrimaryKey());
    }
}