using Orleans;

namespace AElf.Kernel.SmartContract.Grain;

public interface ICleanChainGrain : IGrainWithStringKey
{
    Task CleanCacheAsync(long blockHeight);
}