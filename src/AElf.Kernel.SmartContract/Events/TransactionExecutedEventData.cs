using System.Collections.Generic;
using AElf.Kernel.SmartContract;
using Google.Protobuf.Reflection;

namespace AElf.Kernel.SmartContractExecution.Events;

public class TransactionExecutedEventData
{
    public IReadOnlyList<ServiceDescriptor> Descriptors { get; set; }
    public ITransactionContext TransactionContext { get; set; }
}