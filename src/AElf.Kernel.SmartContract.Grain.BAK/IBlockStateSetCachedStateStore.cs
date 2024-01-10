using AElf.Kernel.SmartContract.Infrastructure;

namespace AElf.Kernel.SmartContract.Orleans;

public interface IBlockStateSetCachedStateStore : INotModifiedCachedStateStore<BlockStateSet>
{
    Task RemoveCacheAsync(long height);
}
public class BlockStateSetCachedStateStore : NotModifiedCachedStateStore<BlockStateSet>,IBlockStateSetCachedStateStore
{
    public BlockStateSetCachedStateStore(IStateStore<BlockStateSet> stateStoreImplementation) : base(stateStoreImplementation)
    {
    }
    
    public Task RemoveCacheAsync(long height)
    {
        var keys = new List<string>();
        foreach (var kv in _cache)
        {
            if (kv.Value.BlockHeight <= height)
            {
                _cache.TryRemove(kv.Key, out _);
            }
        }

        return Task.CompletedTask;
    }
}