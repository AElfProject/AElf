// Use this namespace to avoid compile error in Release mode.
namespace AElf.Kernel.SmartContract
{
#if DEBUG
    public class TransactionExecutedEventData
    {
        public TransactionTrace TransactionTrace { get; set; }
    }
#endif
}