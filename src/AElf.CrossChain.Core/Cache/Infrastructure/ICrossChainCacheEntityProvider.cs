using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Cache.Infrastructure;

public interface ICrossChainCacheEntityProvider
{
    int Size { get; }
    void AddChainCacheEntity(int remoteChainId, long initialTargetHeight);
    List<int> GetCachedChainIds();
    bool TryGetChainCacheEntity(int remoteChainId, out IChainCacheEntity chainCacheEntity);
}

public class CrossChainCacheEntityProvider : ICrossChainCacheEntityProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<int, IChainCacheEntity> _chainCacheEntities = new();

    public int Size => _chainCacheEntities.Count;

    public List<int> GetCachedChainIds()
    {
        return _chainCacheEntities.Keys.ToList();
    }

    public void AddChainCacheEntity(int remoteChainId, long initialTargetHeight)
    {
        var chainCacheEntity = new ChainCacheEntity(remoteChainId, initialTargetHeight);
        _chainCacheEntities.TryAdd(remoteChainId, chainCacheEntity);
    }

    public bool TryGetChainCacheEntity(int remoteChainId, out IChainCacheEntity chainCacheEntity)
    {
        return _chainCacheEntities.TryGetValue(remoteChainId, out chainCacheEntity);
    }
}