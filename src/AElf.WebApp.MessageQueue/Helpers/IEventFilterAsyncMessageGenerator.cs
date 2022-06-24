using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Types;
using AElf.WebApp.MessageQueue.Dtos;
using AElf.WebApp.MessageQueue.Entities;
using Volo.Abp.DependencyInjection;

namespace AElf.WebApp.MessageQueue.Helpers
{
    public interface IEventFilterAsyncMessageGenerator
    {
        Task<Dictionary<Guid, TransactionResultListEto>> GetEventMessageByBlockAsync(int chainId, List<Block> blocks,
            Dictionary<Guid, EventFilterEntity> eventFilters,
            List<EventFilterSet> eventFilterSets,
            CancellationToken ctsToken);
    }

    [ExposeServices(typeof(IEventFilterAsyncMessageGenerator))]
    public class EventFilterAsyncMessageGenerator : EventFilterAsyncMessageGeneratorAbstract,
        IEventFilterAsyncMessageGenerator, ITransientDependency
    {
        private readonly ITransactionResultQueryService _transactionResultQueryService;
        private readonly ITransactionManager _transactionManager;


        public EventFilterAsyncMessageGenerator(
            ITransactionResultQueryService transactionResultQueryService,
            ITransactionManager transactionManager, ITransactionEtoGenerator transactionEtoGenerator) : base(
            transactionEtoGenerator)
        {
            _transactionResultQueryService = transactionResultQueryService;
            _transactionManager = transactionManager;
        }

        protected override async Task<Transaction> GetTransactionAsync(IBlock block, Hash txId)
        {
            return await _transactionManager.GetTransactionAsync(txId);
        }

        protected override async Task<TransactionResult> GetTransactionResultAsync(IBlock block, Hash txId)
        {
            return await _transactionResultQueryService.GetTransactionResultAsync(txId);
        }
    }
}