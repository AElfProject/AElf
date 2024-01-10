using Orleans;

namespace AElf.Kernel.SmartContract.Orleans;

public interface ICleanChainGrain : IGrainWithStringKey
{
    Task CleanCacheAsync(long blockHeight);
}