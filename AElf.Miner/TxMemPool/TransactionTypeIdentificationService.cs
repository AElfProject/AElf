using System.Collections.Generic;
using System.Linq;
using AElf.Kernel;
using AElf.Kernel.Txn;

namespace AElf.Miner.TxMemPool
{
    public class TransactionTypeIdentificationService
    {
        private readonly List<TransactionTypeIdentifier> _transactionTypeValidators;

        public TransactionTypeIdentificationService(List<TransactionTypeIdentifier> transactionTypeValidators)
        {
            _transactionTypeValidators = transactionTypeValidators;
        }


        public bool IsDposTransaction(Transaction transaction)
        {
            return _transactionTypeValidators.Any(validator => validator.IsDposTransaction(transaction));;
        }

        public bool IsCrossChainIndexingTransaction(Transaction transaction)
        {
            return _transactionTypeValidators.Any(validator => validator.IsCrossChainIndexingTransaction(transaction));
        }

        public bool IsSystemTransaction(Transaction transaction)
        {
            return _transactionTypeValidators.Any(validator => validator.IsSystemTransaction(transaction));
        }

        public bool IsClaimFeesTransaction(Transaction transaction)
        {
            throw new System.NotImplementedException();
        }
    }
}