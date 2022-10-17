using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using Orleans;

namespace AElf.Kernel.Orleans.Core;

public interface ISmartContractGrain : IGrainWithStringKey
{
    Task SetExecutiveAsync(IExecutive executive);
    Task ExecuteAsync(TransactionContext transactionContext);
}