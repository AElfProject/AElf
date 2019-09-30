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
    public class TxHubTransactionValidationService : ITransactionValidationService, ITransientDependency
    {
        private readonly IEnumerable<ITransactionValidationProvider> _transactionValidationProviders;

        private readonly IEnumerable<IConstrainedTransactionValidationProvider>
            _constrainedTransactionValidationProviders;

        public ILogger<TxHubTransactionValidationService> Logger { get; set; }

        public TxHubTransactionValidationService(
            IEnumerable<ITransactionValidationProvider> transactionValidationProviders,
            IEnumerable<IConstrainedTransactionValidationProvider> constrainedTransactionValidationProviders)
        {
            _transactionValidationProviders = transactionValidationProviders;
            _constrainedTransactionValidationProviders = constrainedTransactionValidationProviders;

            Logger = NullLogger<TxHubTransactionValidationService>.Instance;
        }

        public async Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            foreach (var provider in _transactionValidationProviders)
            {
                if (!await provider.ValidateTransactionAsync(transaction))
                {
                    return false;
                }
            }

            return true;
        }

        public bool ValidateConstrainedTransaction(Transaction transaction, Hash blockHash)
        {
            return _constrainedTransactionValidationProviders.All(provider =>
            {
                Logger.LogTrace($"Passing {provider.GetType().Name}");
                return provider.ValidateTransaction(transaction, blockHash);
            });
        }

        public void ClearConstrainedTransactionValidationProvider(Hash blockHash)
        {
            foreach (var provider in _constrainedTransactionValidationProviders)
            {
                Logger.LogTrace($"Clearing {provider.GetType().Name}");
                provider.ClearBlockHash(blockHash);
            }
        }
    }
}