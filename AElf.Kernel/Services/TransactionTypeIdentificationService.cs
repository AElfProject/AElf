using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel.Services
{
    public class TransactionTypeIdentificationService : ITransactionTypeIdentificationService
    {
        private readonly List<ITransactionTypeIdentifier> _transactionTypeIdentifiers;

        public TransactionTypeIdentificationService(List<ITransactionTypeIdentifier> transactionTypeIdentifiers)
        {
            _transactionTypeIdentifiers = transactionTypeIdentifiers;
        }

        public bool IsSystemTransaction(int chainId, Transaction transaction)
        {
            return _transactionTypeIdentifiers.Any(typeIdentifier => typeIdentifier.IsSystemTransaction(chainId, transaction));
        }

        public bool CanBeBroadCast(int chainId, Transaction transaction)
        {
            return _transactionTypeIdentifiers.All(typeIdentifier => typeIdentifier.CanBeBroadCast(chainId, transaction));
        }
    }
}