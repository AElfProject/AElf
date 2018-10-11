using System.Threading;
using AElf.Common;

namespace AElf.Kernel
{
    public class TransactionHolder:ITransactionHolderView
    {
        private readonly Transaction _transaction;
        private TxStatus _status;

        private Hash _id;

        public TxStatus Status
        {
            get => _status;
        }

        public Transaction Transaction
        {
            get => _transaction;
        }
        
        public Hash TxId
        {
            get
            {
                if (_id == null)
                {
                    _id = _transaction.GetHash();
                }

                return _id;
            }
        }

        public TransactionHolder(Transaction transaction) : this(transaction, TxStatus.Received)
        {
        }

        public TransactionHolder(Transaction transaction, TxStatus status)
        {
            _transaction = transaction;
            _status = status;
        }

        #region transition state

        private bool TransitionState(TxStatus from, TxStatus to)
        {
            var originalValue = Interlocked.CompareExchange(ref _status, to, from);
            return originalValue == from;
        }

        public bool NeedRevalidating()
        {
            return TransitionState(TxStatus.Validated, TxStatus.Received);
        }
        
        public bool ToValidating()
        {
            return TransitionState(TxStatus.Received, TxStatus.Validating);
        }

        public bool ToValidated()
        {
            return TransitionState(TxStatus.Validating, TxStatus.Validated);
        }

        public bool ToInvalid()
        {
            return TransitionState(TxStatus.Validating, TxStatus.Invalid);
        }
//        public bool ToGrouping()
//        {
//            return TransitionState(TxStatus.Received, TxStatus.Grouping);
//        }
//
//        public bool ToGrouped()
//        {
//            return TransitionState(TxStatus.Received, TxStatus.Grouped);
//        }

        public bool ToExecuting()
        {
            return TransitionState(TxStatus.Validated, TxStatus.Executing);
        }

        public bool RevertExecuting()
        {
            return TransitionState(TxStatus.Executing, TxStatus.Validated);
        }

        public bool ToExecuted()
        {
            return TransitionState(TxStatus.Executing, TxStatus.Executed);
        }

        public bool ToExpired()
        {
            return TransitionState(TxStatus.Validated, TxStatus.Expired);
        }

        #endregion transition state
    }
}