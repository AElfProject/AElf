using AElf.Types;

namespace AElf.Kernel
{
    public partial class TransactionReceipt
    {
        public TransactionReceipt(Transaction transaction)
        {
            TransactionId = transaction.GetHash();
            Transaction = transaction;
        }
    }
}