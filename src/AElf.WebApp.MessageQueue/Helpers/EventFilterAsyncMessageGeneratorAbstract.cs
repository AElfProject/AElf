using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Types;
using AElf.WebApp.MessageQueue.Dtos;
using AElf.WebApp.MessageQueue.Entities;
using AElf.WebApp.MessageQueue.Extensions;

namespace AElf.WebApp.MessageQueue.Helpers
{
    public abstract class EventFilterAsyncMessageGeneratorAbstract
    {
        private readonly ITransactionEtoGenerator _transactionEtoGenerator;

        protected EventFilterAsyncMessageGeneratorAbstract(ITransactionEtoGenerator transactionEtoGenerator)
        {
            _transactionEtoGenerator = transactionEtoGenerator;
        }

        public async Task<Dictionary<Guid, TransactionResultListEto>> GetEventMessageByBlockAsync(int chainId,
            List<Block> blocks,
            Dictionary<Guid, EventFilterEntity> eventFilters,
            List<EventFilterSet> eventFilterSets,
            CancellationToken ctsToken = default)
        {
            var filtersMsgDic = new Dictionary<Guid, TransactionResultListEto>();
            var startHeight = blocks.First().Height;
            var endHeight = blocks.Last().Height;
            foreach (var block in blocks)
            {
                if (ctsToken.IsCancellationRequested)
                {
                    return null;
                }

                var msgDicByBlock =
                    await GetTransactionResultEtoByBlockAsync(block, eventFilters, eventFilterSets, ctsToken);
                if (msgDicByBlock == null)
                {
                    return null;
                }

                foreach (var msgKp in msgDicByBlock)
                {
                    if (!filtersMsgDic.TryGetValue(msgKp.Key, out var transactionResultListEto))
                    {
                        transactionResultListEto = new TransactionResultListEto
                        {
                            StartBlockNumber = startHeight,
                            EndBlockNumber = endHeight,
                            ChainId = chainId,
                            TransactionResults = new Dictionary<string, List<TransactionResultEto>>()
                        };

                        filtersMsgDic[msgKp.Key] = transactionResultListEto;
                    }

                    foreach (var txMsg in msgKp.Value)
                    {
                        if (transactionResultListEto.TransactionResults.TryGetValue(txMsg.TransactionId,
                                out var txList))
                        {
                            txList.Add(txMsg);
                            continue;
                        }

                        transactionResultListEto.TransactionResults.Add(txMsg.TransactionId,
                            new List<TransactionResultEto>
                            {
                                txMsg
                            });
                    }
                }
            }

            return filtersMsgDic;
        }

        private async Task<Dictionary<Guid, List<TransactionResultEto>>> GetTransactionResultEtoByBlockAsync(
            IBlock block,
            Dictionary<Guid, EventFilterEntity> eventFiltersDic,
            IEnumerable<EventFilterSet> eventFilterSets, CancellationToken ctsToken)
        {
            var validEventSetInBlock = eventFilterSets.GetBlockEventFiltersByBloom(block);
            if (!validEventSetInBlock.Any())
            {
                return null;
            }

            var result = new Dictionary<Guid, List<TransactionResultEto>>();
            foreach (var txId in block.TransactionIds)
            {
                if (ctsToken.IsCancellationRequested)
                {
                    return null;
                }

                var eventMsgByPerTransactionDic =
                    await GetTransactionResultEtoByTransactionAsync(block, txId, eventFiltersDic, validEventSetInBlock,
                        ctsToken);

                foreach (var kp in eventMsgByPerTransactionDic)
                {
                    if (result.TryGetValue(kp.Key, out var txEtoList))
                    {
                        txEtoList.Add(kp.Value);
                        continue;
                    }

                    result.Add(kp.Key, new List<TransactionResultEto>
                    {
                        kp.Value
                    });
                }
            }

            return result;
        }

        private async Task<Dictionary<Guid, TransactionResultEto>> GetTransactionResultEtoByTransactionAsync(
            IBlock block,
            Hash txId,
            Dictionary<Guid, EventFilterEntity> eventFilters,
            IEnumerable<EventFilterSet> eventFilterSets, CancellationToken ctsToken)
        {
            var transactionResult = await GetTransactionResultAsync(block, txId);
            var transaction = await GetTransactionAsync(block, txId);
            if (transactionResult == null || transaction == null)
            {
                return null;
            }

            var validEventFilterSets = eventFilterSets.GetTransactionEventFilters(transactionResult);
            if (!validEventFilterSets.Any())
            {
                return null;
            }

            if (ctsToken.IsCancellationRequested)
            {
                return null;
            }

            var result = new Dictionary<Guid, TransactionResultEto>();
            var newAddTxEto =
                _transactionEtoGenerator.GetTransactionEto(block, transactionResult, transaction);
            var toAddress = transaction.To.ToBase58();
            foreach (var validEventFilter in validEventFilterSets)
            {
                foreach (var filterId in validEventFilter.FilterIds)
                {
                    if (result.ContainsKey(filterId))
                    {
                        continue;
                    }

                    var filterDetail = eventFilters[filterId];
                    var eventDetail = filterDetail.EventDetails.First(x =>
                        x.Address == validEventFilter.Address && x.Names.Contains(validEventFilter.Name));

                    if (IsEventIncludedInTransaction(eventDetail, toAddress))
                    {
                        result.Add(filterId, newAddTxEto);
                    }
                }
            }

            return result;
        }

        protected abstract Task<Transaction> GetTransactionAsync(IBlock block, Hash txId);
        protected abstract Task<TransactionResult> GetTransactionResultAsync(IBlock block, Hash txId);

        protected virtual bool IsEventIncludedInTransaction(EventDetail eventDetail, string toAddress)
        {
            if (eventDetail.ToAddress.IsNullOrEmpty())
            {
                return true;
            }

            return eventDetail.ToAddress == toAddress;
        }
    }
}