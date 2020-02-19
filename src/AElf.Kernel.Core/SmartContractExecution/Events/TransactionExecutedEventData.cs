using AElf.Types;

namespace AElf.Kernel.SmartContractExecution.Events
{
    //TODO: if TransactionExecutedEventData is only for Debug, TransactionExecutedEventData should also wrap in #DEBUG

    public class TransactionExecutedEventData
    {
        public TransactionTrace TransactionTrace { get; set; }
    }
}