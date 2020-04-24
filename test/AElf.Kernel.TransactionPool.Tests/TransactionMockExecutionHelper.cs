using AElf.Types;

namespace AElf.Kernel.TransactionPool
{
    public class TransactionMockExecutionHelper
    {
        private TransactionResultStatus _transactionResultStatus;

        internal void SetTransactionResultStatus(TransactionResultStatus transactionResultStatus)
        {
            _transactionResultStatus = transactionResultStatus;
        }

        internal TransactionResultStatus GetTransactionResultStatus()
        {
            return _transactionResultStatus;
        }
    }
}