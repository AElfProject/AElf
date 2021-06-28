using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Parallel
{
    public class SimpleTransactionGrouper: ITransactionGrouper, ISingletonDependency
    {
        private ISmartContractAddressService _smartContractAddressService;
        public ILogger<SimpleTransactionGrouper> Logger { get; set; }

        public SimpleTransactionGrouper(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
            Logger = NullLogger<SimpleTransactionGrouper>.Instance;
        }

        public async Task<GroupedTransactions> GroupAsync(IChainContext chainContext, List<Transaction> transactions)
        {
            Logger.LogTrace("Begin SimpleTransactionGrouper.GroupAsync");
            
            var groupedTransactions = transactions.GroupBy(t => t.From).Select(g => g.ToList()).ToList();
            
            Logger.LogTrace("End SimpleTransactionGrouper.GroupAsync");

            Logger.LogDebug($"From {transactions.Count} transactions, grouped {groupedTransactions.Sum(p=>p.Count)} txs into " +
                            $"{groupedTransactions.Count} groups. ");

            return new GroupedTransactions
            {
                Parallelizables = groupedTransactions
            };
        }
    }
}