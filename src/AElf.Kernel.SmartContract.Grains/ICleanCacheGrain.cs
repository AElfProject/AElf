using Orleans;

namespace AElf.Kernel.SmartContract.Grains;

public interface ICleanCacheGrain : IGrainWithIntegerKey
{
    Task CleanCacheAsync(long blockHeight);
}