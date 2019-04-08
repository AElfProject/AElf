using System.Collections.Generic;
using Google.Protobuf.Reflection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IExecutionPlugin
    {
        IEnumerable<Transaction> GetPreTransactions(IReadOnlyList<ServiceDescriptor> descriptors);
    }
}