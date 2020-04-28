using AElf.Types;

namespace AElf.Kernel.TransactionPool
{
    public class TransactionAcceptedEvent
    {
        public int ChainId { get; set; }
        public Transaction Transaction { get; set; }
    }
}