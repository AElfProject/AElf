using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Grain;

public interface IPlainTransactionExecutingGrainProvider
{
    int TryGetGrainId();
    
    void AddGrainId(int id);
}

public class PlainTransactionExecutingGrainProvider : IPlainTransactionExecutingGrainProvider, ISingletonDependency
{
    private int _poolCurrentId = 0;
    private readonly ILogger<PlainTransactionExecutingGrainProvider> _logger;
    private readonly ConcurrentBag<int> _pool = new();

    public PlainTransactionExecutingGrainProvider(ILogger<PlainTransactionExecutingGrainProvider> logger)
    {
        _logger = logger;
    }
    public int TryGetGrainId()
    {
        if (!_pool.TryTake(out var id))
        {
            if (_poolCurrentId == int.MaxValue)
            {
                id = 0;
            }
            else
            {
                id = _poolCurrentId + 1;
            }
            _poolCurrentId = id;
            _logger.LogDebug("Create new Id for {id}",id);
        }
        return id;
    }

    public void AddGrainId(int id)
    {
        _pool.Add(id);
    }
}