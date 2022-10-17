namespace AElf.Kernel.SmartContract.Orleans.Application;

public interface IOrleansTransactionExecutingClientService
{
    Task ExecuteGrain(string grainKey, TransactionContext transactionContext);
}