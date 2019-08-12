using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Types;
using Google.Protobuf.Reflection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IPostExecutionPlugin
    {
        Task<IEnumerable<Transaction>> GetPostTransactionsAsync(IReadOnlyList<ServiceDescriptor> descriptors,
            ITransactionContext transactionContext);
    }
}