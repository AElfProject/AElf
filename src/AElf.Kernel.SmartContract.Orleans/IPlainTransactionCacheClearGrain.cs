using AElf.Kernel.SmartContract.Application;
using Orleans;

namespace AElf.Kernel.SmartContract.Orleans;

public interface IPlainTransactionCacheClearGrain : IGrainWithStringKey
{
    Task CleanChainAsync(long blockStateHeight);
}