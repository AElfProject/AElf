using AElf.Types;

namespace AElf.Kernel.TransactionPool
{
    public class TransactionExecutionValidationFailedEvent
    {
        public Hash TransactionId { get; set; }
    }
}