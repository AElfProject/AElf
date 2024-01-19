using Orleans;

namespace AElf.Kernel.SmartContract.Orleans;

public interface ICleanCacheGrain : IGrainWithIntegerKey
{
    Task CleanCacheAsync(long blockHeight);
}