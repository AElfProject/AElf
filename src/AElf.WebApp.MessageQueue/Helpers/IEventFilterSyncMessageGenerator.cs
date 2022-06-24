using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain;
using AElf.Types;
using AElf.WebApp.MessageQueue.Entities;
using Volo.Abp.DependencyInjection;

namespace AElf.WebApp.MessageQueue.Helpers
{
    public interface IEventFilterSyncMessageGenerator : IEventFilterAsyncMessageGenerator
    {
        Task<Dictionary<Guid, TransactionResultListEto>> GetEventMessageByBlockAsync(int chainId,
            List<BlockExecutedSet> blockExecutedSets, List<EventFilterEntity> eventFilters);
    }

    public class EventFilterSyncMessageGenerator : EventFilterAsyncMessageGeneratorAbstract,
        IEventFilterSyncMessageGenerator,
        ITransientDependency
    {
        private Dictionary<Hash, BlockExecutedSet> _blockExecutedSetDic = null;

        public async Task<Dictionary<Guid, TransactionResultListEto>> GetEventMessageByBlockAsync(int chainId,
            List<BlockExecutedSet> blockExecutedSets,
            List<EventFilterEntity> eventFilters)
        {
            SetEventInfoSource(blockExecutedSets);
            var eventFilterSets = EventFilterSetHelper.TransferToEventFilterSet(eventFilters);
            var blocks = blockExecutedSets.Select(b => b.Block).ToList();
            var eventFiltersDic = eventFilters.ToDictionary(k => k.Id, v => v);
            return await base.GetEventMessageByBlockAsync(chainId, blocks, eventFiltersDic, eventFilterSets);
        }

        private void SetEventInfoSource(IEnumerable<BlockExecutedSet> blockExecutedSets)
        {
            _blockExecutedSetDic = blockExecutedSets.ToDictionary(k => k.GetHash(), v => v);
        }

        public EventFilterSyncMessageGenerator(ITransactionEtoGenerator transactionEtoGenerator) :
            base(
                transactionEtoGenerator)
        {
        }

        protected override Task<Transaction> GetTransactionAsync(IBlock block, Hash txId)
        {
            return Task.FromResult(_blockExecutedSetDic[block.GetHash()].TransactionMap[txId]);
        }

        protected override Task<TransactionResult> GetTransactionResultAsync(IBlock block, Hash txId)
        {
            return Task.FromResult(_blockExecutedSetDic[block.GetHash()].TransactionResultMap[txId]);
        }
    }
}