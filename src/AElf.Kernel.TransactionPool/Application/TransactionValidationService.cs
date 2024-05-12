using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Txn.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.TransactionPool.Application;

[Ump]
public class TransactionValidationService : ITransactionValidationService, ITransientDependency
{
    private readonly IBlockchainService _blockchainService;
    private readonly IEnumerable<ITransactionValidationProvider> _transactionValidationProviders;
    private readonly ActivitySource _activitySource;

    public TransactionValidationService(
        IEnumerable<ITransactionValidationProvider> transactionValidationProviders,
        IBlockchainService blockchainService,
        Instrumentation instrumentation)
    {
        _transactionValidationProviders = transactionValidationProviders;
        _blockchainService = blockchainService;
        _activitySource = instrumentation.ActivitySource;

        Logger = NullLogger<TransactionValidationService>.Instance;
    }

    public ILogger<TransactionValidationService> Logger { get; set; }

    /// <summary>
    ///     Validate txs before they enter tx hub.
    /// </summary>
    /// <param name="chainContext"></param>
    /// <param name="transaction"></param>
    /// <returns></returns>
    public async Task<bool> ValidateTransactionWhileCollectingAsync(IChainContext chainContext,
        Transaction transaction)
    {
        using var activity = _activitySource.StartActivity();

        foreach (var provider in _transactionValidationProviders)
        {
            if (await provider.ValidateTransactionAsync(transaction, chainContext)) continue;
            Logger.LogDebug(
                $"[ValidateTransactionWhileCollectingAsync]Transaction {transaction.GetHash()} validation failed in {provider.GetType()}");
            return false;
        }

        return true;
    }

    public async Task<bool> ValidateTransactionWhileSyncingAsync(Transaction transaction)
    {
        foreach (var provider in _transactionValidationProviders)
        {
            if (!provider.ValidateWhileSyncing ||
                await provider.ValidateTransactionAsync(transaction)) continue;
            Logger.LogDebug(
                $"[ValidateTransactionWhileSyncingAsync]Transaction {transaction.GetHash()} validation failed in {provider.GetType()}");
            return false;
        }

        return true;
    }
}