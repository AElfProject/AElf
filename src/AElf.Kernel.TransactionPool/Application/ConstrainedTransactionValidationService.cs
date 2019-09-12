using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.TransactionPool.Application
{
    public class ConstrainedTransactionValidationService : IConstrainedTransactionValidationService
    {
        private readonly IEnumerable<IConstrainedTransactionValidationProvider> _transactionValidationProviders;

        public ConstrainedTransactionValidationService(
            IEnumerable<IConstrainedTransactionValidationProvider> transactionValidationProviders)
        {
            _transactionValidationProviders = transactionValidationProviders;
        }

        public bool ValidateTransaction(Transaction transaction, Hash blockHash)
        {
            foreach (var provider in _transactionValidationProviders)
            {
                if (!provider.ValidateTransaction(transaction, blockHash))
                {
                    return false;
                }

                provider.ClearBlockHash(blockHash);
            }

            return true;
        }
    }
}