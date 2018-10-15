using AElf.Kernel;

namespace AElf.Node.EventMessages
{
    public class TxReceived
    {
        public Transaction Transaction { get; private set; }

        public TxReceived(Transaction transaction)
        {
            Transaction = transaction;
        }
    }
}