using System.Collections.Generic;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.TransactionPool.Application
{
    public class ConstrainedTransactionValidationService : IConstrainedTransactionValidationService, ITransientDependency
    {
        private readonly IEnumerable<IConstrainedTransactionValidationProvider> _transactionValidationProviders;

        public ILogger<ConstrainedTransactionValidationService> Logger { get; set; }

        public ConstrainedTransactionValidationService(
            IEnumerable<IConstrainedTransactionValidationProvider> transactionValidationProviders)
        {
            _transactionValidationProviders = transactionValidationProviders;
            Logger = NullLogger<ConstrainedTransactionValidationService>.Instance;
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