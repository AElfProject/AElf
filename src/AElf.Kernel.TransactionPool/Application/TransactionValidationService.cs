using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Txn.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.TransactionPool.Application
{
    public class TransactionValidationService : ITransactionValidationService, ITransientDependency
    {
        private readonly IEnumerable<ITransactionValidationProvider> _transactionValidationProviders;

        public ILogger<TransactionValidationService> Logger { get; set; }

        public TransactionValidationService(
            IEnumerable<ITransactionValidationProvider> transactionValidationProviders)
        {
            _transactionValidationProviders = transactionValidationProviders;

            Logger = NullLogger<TransactionValidationService>.Instance;
        }

        public async Task<bool> ValidateTransactionWhileCollectingAsync(Transaction transaction)
        {
            foreach (var provider in _transactionValidationProviders)
            {
                if (await provider.ValidateTransactionAsync(transaction)) continue;
                Logger.LogWarning(
                    $"[ValidateTransactionWhileCollectingAsync]Transaction {transaction.GetHash()} validation failed in {provider.GetType()}");
                return false;
            }

            return true;
        }

        public async Task<bool> ValidateTransactionWhileSyncingAsync(Transaction transaction)
        {
            foreach (var provider in _transactionValidationProviders)
            {
                if (!provider.ValidateWhileSyncing || await provider.ValidateTransactionAsync(transaction)) continue;
                Logger.LogWarning(
                    $"[ValidateTransactionWhileSyncingAsync]Transaction {transaction.GetHash()} validation failed in {provider.GetType()}");
                return false;
            }

            return true;
        }
    }
}