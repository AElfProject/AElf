using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Parallel
{
    public class SimpleTransactionGrouper: ITransactionGrouper, ISingletonDependency
    {
        public ILogger<SimpleTransactionGrouper> Logger { get; set; }

        public SimpleTransactionGrouper()
        {
            Logger = NullLogger<SimpleTransactionGrouper>.Instance;
        }

        public async Task<GroupedTransactions> GroupAsync(IChainContext chainContext, List<Transaction> transactions)
        {
            Logger.LogTrace("Begin SimpleTransactionGrouper.GroupAsync");
            
            var groupedTransactions = transactions.GroupBy(t => t.From).Select(g => g.ToList()).ToList();

            // var groupCount = string.Join(",", groupedTransactions.Select(p => p.Count));
            // Logger.LogInformation($"Simple GroupCount: {groupCount}");
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