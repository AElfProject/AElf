using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Sdk;
using Google.Protobuf.Reflection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IExecutionPlugin
    {
        Task<IEnumerable<Transaction>> GetPreTransactionsAsync(IReadOnlyList<ServiceDescriptor> descriptors,
            ITransactionContext transactionContext);
    }
}