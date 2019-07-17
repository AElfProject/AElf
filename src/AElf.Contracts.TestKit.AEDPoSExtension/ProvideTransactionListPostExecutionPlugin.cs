using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Types;
using Google.Protobuf.Reflection;

namespace AElf.Contracts.TestKet.AEDPoSExtension
{
    public class ProvideTransactionListPostExecutionPlugin : IPostExecutionPlugin
    {
        private readonly ITransactionListProvider _transactionListProvider;

        public ProvideTransactionListPostExecutionPlugin(ITransactionListProvider transactionListProvider)
        {
            _transactionListProvider = transactionListProvider;
        }

        public async Task<IEnumerable<Transaction>> GetPostTransactionsAsync(IReadOnlyList<ServiceDescriptor> descriptors,
            ITransactionContext transactionContext)
        {
            var transactionList = await _transactionListProvider.GetTransactionListAsync();
            return transactionList;
        }
    }
}