using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;

namespace AElf.Kernel.TransactionPool.Application
{
    public class TransactionValidationForTxHubService : ITransactionValidationService
    {
        private readonly IEnumerable<ITransactionValidationProvider> _transactionValidationProviders;

        private readonly IEnumerable<IConstrainedTransactionValidationProvider>
            _constrainedTransactionValidationProviders;

        public TransactionValidationForTxHubService(
            IEnumerable<ITransactionValidationProvider> transactionValidationProviders,
            IEnumerable<IConstrainedTransactionValidationProvider> constrainedTransactionValidationProviders)
        {
            _transactionValidationProviders = transactionValidationProviders;
            _constrainedTransactionValidationProviders = constrainedTransactionValidationProviders;
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
                provider.ValidateTransaction(transaction, blockHash));
        }
    }
}