using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;

namespace AElf.CrossChain
{
    public class CrossChainBlockExtraDataProvider : IBlockExtraDataProvider
    {
        private readonly ITransactionResultManager _transactionResultManager;

        public CrossChainBlockExtraDataProvider(ITransactionResultManager transactionResultManager)
        {
            _transactionResultManager = transactionResultManager;
        }

        public async Task FillExtraDataAsync(int chainId, Block block)
        {
            if (!CrossChainEventHelper.TryGetLogEventInBlock(block, out var logEvent))
                return;
            foreach (var txId in block.Body.Transactions)
            {
                var res = await _transactionResultManager.GetTransactionResultAsync(txId);
                foreach (var eventLog in res.Logs)
                {
                    if (!logEvent.Topics.Equals(eventLog.Topics))
                        continue;
                    object[] indexingEventData = CrossChainEventHelper.ExtractCrossChainBlockDataFromEvent(eventLog);
                    block.Header.BlockExtraData.SideChainTransactionsRoot = (Hash) indexingEventData[0];
                    return;
                }
            }
        }
    }
}