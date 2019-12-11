using System.Collections.Generic;
using System.Linq;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public class InlineTransactionValidationService : IInlineTransactionValidationService
    {
        private readonly IEnumerable<IInlineTransactionValidationProvider> _inlineTransactionValidationProviders;

        public InlineTransactionValidationService(
            IEnumerable<IInlineTransactionValidationProvider> inlineTransactionValidationProviders)
        {
            _inlineTransactionValidationProviders = inlineTransactionValidationProviders;
        }

        public bool Validate(Transaction transaction)
        {
            return _inlineTransactionValidationProviders.Any() && _inlineTransactionValidationProviders.Any(
                       inlineTransactionValidationProvider =>
                           inlineTransactionValidationProvider.Validate(transaction));
        }
    }
}