namespace AElf.Kernel.Miner.Application;

[Ump]
public class SystemTransactionGenerationService : ISystemTransactionGenerationService
{
    private readonly IEnumerable<ISystemTransactionGenerator> _systemTransactionGenerators;

    // TODO: A better strategy to control system transaction order.
    public SystemTransactionGenerationService(IEnumerable<ISystemTransactionGenerator> systemTransactionGenerators)
    {
        _systemTransactionGenerators = systemTransactionGenerators;
    }

    public ILogger<SystemTransactionGenerationService> Logger { get; set; }

    public async Task<List<Transaction>> GenerateSystemTransactionsAsync(Address from, long preBlockHeight,
        Hash preBlockHash)
    {
        var generatedTransactions = new List<Transaction>();
        foreach (var generator in _systemTransactionGenerators)
            generatedTransactions.AddRange(
                await generator.GenerateTransactionsAsync(from, preBlockHeight, preBlockHash));

        return generatedTransactions;
    }
}