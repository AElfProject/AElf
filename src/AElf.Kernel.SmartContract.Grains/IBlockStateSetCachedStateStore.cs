using AElf.Kernel.SmartContract.Infrastructure;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.SmartContract.Grains;

public interface IBlockStateSetCachedStateStore : INotModifiedCachedStateStore<BlockStateSet>
{
    Task RemoveCacheAsync(long height);
}
public class BlockStateSetCachedStateStore : NotModifiedCachedStateStore<BlockStateSet>,IBlockStateSetCachedStateStore
{
    private readonly ILogger<BlockStateSetCachedStateStore> _logger;
    public BlockStateSetCachedStateStore(IStateStore<BlockStateSet> stateStoreImplementation,  ILogger<BlockStateSetCachedStateStore> logger) : base(stateStoreImplementation)
    {
        _logger = logger;
    }
    
    public Task RemoveCacheAsync(long height)
    {
        _logger.LogDebug("BlockStateSetCachedStateStore.RemoveCacheAsync-start height: {0} cacheCount:{1}",
            height,_cache.Count);
        var keys = new List<string>();
        foreach (var kv in _cache)
        {
            if (kv.Value.BlockHeight <= height)
            {
                _cache.TryRemove(kv.Key, out _);
            }
        }
        _logger.LogDebug("BlockStateSetCachedStateStore.RemoveCacheAsync-end height: {0} cacheCount:{1}",
            height,_cache.Count);
        return Task.CompletedTask;
    }
}