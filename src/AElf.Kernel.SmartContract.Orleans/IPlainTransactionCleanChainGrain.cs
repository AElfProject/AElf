using Orleans;

namespace AElf.Kernel.SmartContract.Orleans;

public interface IPlainTransactionCleanChainGrain : IGrainWithStringKey
{
    Task CleanChainStateAsync(long blockStateHeight);
}