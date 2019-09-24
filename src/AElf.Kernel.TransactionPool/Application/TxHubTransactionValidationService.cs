using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.TransactionPool.Application
{
    public class TxHubTransactionValidationService : ITransactionValidationService, ITransientDependency
    {
        private readonly IEnumerable<ITransactionValidationProvider> _transactionValidationProviders;

        private readonly IConstrainedTransactionValidationService _constrainedTransactionValidationService;

        public TxHubTransactionValidationService(
            IEnumerable<ITransactionValidationProvider> transactionValidationProviders,
            IConstrainedTransactionValidationService constrainedTransactionValidationService)
        {
            _transactionValidationProviders = transactionValidationProviders;
            _constrainedTransactionValidationService = constrainedTransactionValidationService;
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
            return _constrainedTransactionValidationService.ValidateTransaction(transaction, blockHash);
        }
    }
}