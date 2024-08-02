using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Txn.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.TransactionPool.Application;

public class TransactionValidationService : ITransactionValidationService, ITransientDependency
{
    private readonly IBlockchainService _blockchainService;
    private readonly IEnumerable<ITransactionValidationProvider> _transactionValidationProviders;

    public TransactionValidationService(
        IEnumerable<ITransactionValidationProvider> transactionValidationProviders,
        IBlockchainService blockchainService)
    {
        _transactionValidationProviders = transactionValidationProviders;
        _blockchainService = blockchainService;

        Logger = NullLogger<TransactionValidationService>.Instance;
    }

    public ILogger<TransactionValidationService> Logger { get; set; }

    /// <summary>
    ///     Validate txs before they enter tx hub.
    /// </summary>
    /// <param name="chainContext"></param>
    /// <param name="transaction"></param>
    /// <returns></returns>
    public async Task<bool> ValidateTransactionWhileCollectingAsync(IChainContext chainContext, Transaction transaction)
    {
        var validationTasks = _transactionValidationProviders.AsParallel().Select(async provider =>
        {
            if (await provider.ValidateTransactionAsync(transaction, chainContext)) return true;
            Logger.LogDebug(
                $"[ValidateTransactionWhileCollectingAsync]Transaction {transaction.GetHash()} validation failed in {provider.GetType()}");
            return false;
        });

        var results = await Task.WhenAll(validationTasks);
        return results.All(result => result);
    }

    public async Task<bool> ValidateTransactionWhileSyncingAsync(Transaction transaction)
    {
        var validationTasks = _transactionValidationProviders.AsParallel()
            .Where(provider => provider.ValidateWhileSyncing).Select(async provider =>
            {
                if (await provider.ValidateTransactionAsync(transaction)) return true;
                Logger.LogDebug(
                    $"[ValidateTransactionWhileSyncingAsync]Transaction {transaction.GetHash()} validation failed in {provider.GetType()}");
                return false;
            });

        var results = await Task.WhenAll(validationTasks);
        return results.All(result => result);
    }
}