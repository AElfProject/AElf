using AElf.Kernel;

namespace AElf.ChainController.EventMessages
{
    public sealed class IncomingTransaction
    {
        public IncomingTransaction(Transaction transaction)
        {
            Transaction = transaction;
        }

        public Transaction Transaction { get; }
    }
}