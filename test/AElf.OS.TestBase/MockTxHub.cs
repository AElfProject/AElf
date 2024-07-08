using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.TransactionPool.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Types;

namespace AElf.OS;

public class MockTxHub : ITxHub
{
    private readonly Dictionary<Hash, Transaction> _allTransactions = new();

    private readonly IBlockchainService _blockchainService;
    private readonly ITransactionManager _transactionManager;
    private Hash _bestChainHash = Hash.Empty;

    private long _bestChainHeight = AElfConstants.GenesisBlockHeight - 1;

    public MockTxHub(ITransactionManager transactionManager, IBlockchainService blockchainService)
    {
        _transactionManager = transactionManager;
        _blockchainService = blockchainService;
    }

    public Task<ExecutableTransactionSet> GetExecutableTransactionSetAsync(Hash blockHash,
        int transactionCount)
    {
        return Task.FromResult(new ExecutableTransactionSet
        {
            PreviousBlockHash = _bestChainHash,
            PreviousBlockHeight = _bestChainHeight,
            Transactions = _allTransactions.Values.ToList()
        });
    }

    public async Task AddTransactionsAsync(IEnumerable<Transaction> transactions)
    {
        var txs = transactions.ToList();
        foreach (var transaction in txs)
        {
            if (_allTransactions.ContainsKey(transaction.GetHash()))
                continue;
            _allTransactions.Add(transaction.GetHash(), transaction);
        }

        await _transactionManager.AddTransactionsAsync(txs);
    }

    public Task CleanByTransactionIdsAsync(IEnumerable<Hash> transactionIds)
    {
        CleanTransactions(transactionIds);

        return Task.CompletedTask;
    }

    public async Task UpdateTransactionPoolByBestChainAsync(Hash bestChainHash, long bestChainHeight)
    {
        _bestChainHeight = bestChainHeight;
        _bestChainHash = bestChainHash;
        await Task.CompletedTask;
    }

    public async Task CleanByHeightAsync(long height)
    {
        await Task.CompletedTask;
    }

    public async Task<QueuedTransaction> GetQueuedTransactionAsync(Hash transactionId)
    {
        if (!_allTransactions.TryGetValue(transactionId, out var transaction)) return null;

        var queuedTransaction = await Task.FromResult(new QueuedTransaction
        {
            TransactionId = transactionId,
            Transaction = transaction
        });

        return queuedTransaction;
    }

    public Task<TransactionPoolStatus> GetTransactionPoolStatusAsync()
    {
        return Task.FromResult(new TransactionPoolStatus
        {
            AllTransactionCount = _allTransactions.Count
        });
    }

    private void CleanTransactions(IEnumerable<Hash> transactionIds)
    {
        foreach (var transactionId in transactionIds) _allTransactions.Remove(transactionId, out _);
    }
}