using Orleans;

namespace AElf.Kernel.SmartContract.Grains;

public interface ICleanCacheGrain : IGrainWithStringKey
{
    Task CleanCacheAsync(long blockHeight);
}