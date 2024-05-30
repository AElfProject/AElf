using System.Linq;

namespace AElf.Kernel.Miner.Application;

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
        // var generatedTransactions = new List<Transaction>();
        // foreach (var generator in _systemTransactionGenerators)
        //     generatedTransactions.AddRange(
        //         await generator.GenerateTransactionsAsync(from, preBlockHeight, preBlockHash));
        //
        // return generatedTransactions;
        
        
        var generatedTransactions = new List<Transaction>();
        var transactionGenerationTasks = _systemTransactionGenerators
            .Select(generator => generator.GenerateTransactionsAsync(from, preBlockHeight, preBlockHash))
            .ToList();
        
        var generatedTransactionsList = await Task.WhenAll(transactionGenerationTasks);
        
        foreach (var transactions in generatedTransactionsList)
        {
            generatedTransactions.AddRange(transactions);
        }
        
        return generatedTransactions;
    }
}