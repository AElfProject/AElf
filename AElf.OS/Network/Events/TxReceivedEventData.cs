using AElf.Kernel;

namespace AElf.OS.Network.Events
{
    public class TxReceivedEventData
    {
        public Transaction Transaction { get; private set; }

        public TxReceivedEventData(Transaction tx)
        {
            Transaction = tx;
        }
    }
}