using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Orleans;

public interface IPlainTransactionExecutingGrainProvider
{
    int TryGetGrainId(string type);
    
    void AddGrainId(string type, int id);
}

public class PlainTransactionExecutingGrainProvider : IPlainTransactionExecutingGrainProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<int>> _poolDictionary = new();
    private readonly ConcurrentDictionary<string, int> _poolCurrentId = new();
    private readonly int _initialIdCount = 10;
    private readonly ILogger<PlainTransactionExecutingGrainProvider> _logger;

    public PlainTransactionExecutingGrainProvider(ILogger<PlainTransactionExecutingGrainProvider> logger)
    {
        _logger = logger;
    }
    public int TryGetGrainId(string type)
    {
        if (!_poolDictionary.TryGetValue(type, out var pool))
        {
            pool = new ConcurrentBag<int>();
            for (int i = 0; i < _initialIdCount; i++)
            {
                pool.Add(i);
            }
            _poolDictionary[type] = pool;
            _poolCurrentId[type] = pool.Count-1;
            _logger.LogDebug("Create new pool for {type}",type);
        }

        if (!pool.TryTake(out var id))
        {
            if (_poolCurrentId[type] == int.MaxValue)
            {
                id = 0;
            }
            else
            {
                id = _poolCurrentId[type] + 1;
            }
            _poolCurrentId[type] = id;
            pool.Add(id);
            _logger.LogDebug("Create new Id for {type}-{id}",type,id);
        }
        return id;
    }

    public void AddGrainId(string type, int id)
    {
        if (!_poolDictionary.TryGetValue(type, out var pool))
        {
            pool.Add(id);
        }
    }
}