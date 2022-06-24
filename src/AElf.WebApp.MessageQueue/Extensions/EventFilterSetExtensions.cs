using System.Collections.Generic;
using System.Linq;
using AElf.Kernel;
using AElf.Types;
using AElf.WebApp.MessageQueue.Dtos;

namespace AElf.WebApp.MessageQueue.Extensions
{
    public static class EventFilterSetExtensions
    {
        public static List<EventFilterSet> GetBlockEventFiltersByBloom(this IEnumerable<EventFilterSet> eventFilterSets,
            IBlock block)
        {
            var blockBloom = new Bloom(block.Header.Bloom.ToByteArray());
            return eventFilterSets.Where(x => x.Bloom.IsIn(blockBloom)).ToList();
        }

        public static List<EventFilterSet> GetTransactionEventFilters(
            this IEnumerable<EventFilterSet> eventFilterSets,
            TransactionResult transactionResult)
        {
            return eventFilterSets.Where(x => x.IsIncludedInTransaction(transactionResult)).ToList();
        }

        public static bool IsIncludedInTransaction(this EventFilterSet eventFilterSet,
            TransactionResult transactionResult)
        {
            var transactionBloom = new Bloom(transactionResult.Bloom.ToByteArray());
            return eventFilterSet.Bloom.IsIn(transactionBloom) &&
                   eventFilterSet.IsInTransactionByEventInfo(transactionResult);
        }

        public static bool IsInTransactionByEventInfo(this EventFilterSet eventFilterSet,
            TransactionResult transactionResult)
        {
            return transactionResult.Logs.Any(x =>
                x.Address.ToBase58() == eventFilterSet.Address && x.Name == eventFilterSet.Name);
        }
    }
}