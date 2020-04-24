using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract;
using AElf.Types;
using Google.Protobuf.Reflection;

namespace AElf.Contracts.TestKet.AEDPoSExtension
{
    public class ProvideTransactionListPostExecutionPlugin : IPostExecutionPlugin
    {
        private readonly ITransactionListProvider _transactionListProvider;
        private readonly ISmartContractAddressService _smartContractAddressService;

        public ProvideTransactionListPostExecutionPlugin(ITransactionListProvider transactionListProvider,
            ISmartContractAddressService smartContractAddressService)
        {
            _transactionListProvider = transactionListProvider;
            _smartContractAddressService = smartContractAddressService;
        }

        public async Task<IEnumerable<Transaction>> GetPostTransactionsAsync(
            IReadOnlyList<ServiceDescriptor> descriptors,
            ITransactionContext transactionContext)
        {
            return transactionContext.Transaction.To ==
                   await _smartContractAddressService.GetAddressByContractNameAsync(new ChainContext
                   {
                       BlockHash = transactionContext.PreviousBlockHash,
                       BlockHeight = transactionContext.BlockHeight - 1,
                       StateCache = transactionContext.StateCache
                   }, ConsensusSmartContractAddressNameProvider.StringName) &&
                   new List<string>
                   {
                       "FirstRound",
                       "UpdateValue",
                       "UpdateTinyBlockInformation",
                       "NextRound",
                       "NextTerm"
                   }.Contains(transactionContext.Transaction.MethodName)
                ? await _transactionListProvider.GetTransactionListAsync()
                : new List<Transaction>();
        }
    }
}