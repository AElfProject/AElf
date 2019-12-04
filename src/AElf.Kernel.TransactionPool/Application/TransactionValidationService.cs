using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.TransactionPool.Application
{
    public class TransactionValidationService : ITransactionValidationService, ITransientDependency
    {
        private readonly IEnumerable<ITransactionValidationProvider> _transactionValidationProviders;

        private readonly IEnumerable<IConstrainedTransactionValidationProvider>
            _constrainedTransactionValidationProviders;

        public ILogger<TransactionValidationService> Logger { get; set; }

        public TransactionValidationService(
            IEnumerable<ITransactionValidationProvider> transactionValidationProviders,
            IEnumerable<IConstrainedTransactionValidationProvider> constrainedTransactionValidationProviders)
        {
            _transactionValidationProviders = transactionValidationProviders;
            _constrainedTransactionValidationProviders = constrainedTransactionValidationProviders;

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

        public bool ValidateConstrainedTransaction(Transaction transaction, Hash blockHash)
        {
            return _constrainedTransactionValidationProviders.All(provider =>
                provider.ValidateTransaction(transaction, blockHash));
        }

        public void ClearConstrainedTransactionValidationProvider(Hash blockHash)
        {
            foreach (var provider in _constrainedTransactionValidationProviders)
            {
                provider.ClearBlockHash(blockHash);
            }
        }
    }
}