using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Types;
using Google.Protobuf.Reflection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IPreExecutionPlugin
    {
        Task<IEnumerable<Transaction>> GetPreTransactionsAsync(IReadOnlyList<ServiceDescriptor> descriptors,
            ITransactionContext transactionContext);
    }
}