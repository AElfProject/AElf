using System.Collections.Generic;
using System.Linq;
using AElf.Kernel;
using AElf.Kernel.Txn;

namespace AElf.Kernel.Txn
{
    public class TransactionTypeIdentificationService : ITransactionTypeIdentificationService
    {
        private readonly List<ITransactionTypeIdentifier> _transactionTypeIdentifiers;

        public TransactionTypeIdentificationService(List<ITransactionTypeIdentifier> transactionTypeIdentifiers)
        {
            _transactionTypeIdentifiers = transactionTypeIdentifiers;
        }

        public bool IsSystemTransaction(Transaction transaction)
        {
            return _transactionTypeIdentifiers.Any(typeIdentifier => typeIdentifier.IsSystemTransaction(transaction));
        }

        public bool CanBeBroadCast(Transaction transaction)
        {
            return _transactionTypeIdentifiers.All(typeIdentifier => typeIdentifier.CanBeBroadCast(transaction));
        }
    }
}