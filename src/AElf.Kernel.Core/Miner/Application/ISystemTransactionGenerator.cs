namespace AElf.Kernel.Miner.Application;

public interface ISystemTransactionGenerator
{
    Task<List<Transaction>> GenerateTransactionsAsync(Address from, long preBlockHeight, Hash preBlockHash);
}