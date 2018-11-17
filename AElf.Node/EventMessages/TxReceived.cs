using AElf.Kernel;

namespace AElf.Node.EventMessages
{
    public class TxReceived
    {
        public Transaction Transaction { get; }

        public TxReceived(Transaction transaction)
        {
            Transaction = transaction;
        }
    }
}