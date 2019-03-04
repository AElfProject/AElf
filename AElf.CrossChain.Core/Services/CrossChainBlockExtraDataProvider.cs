using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;

namespace AElf.CrossChain
{
    public class CrossChainBlockExtraDataProvider : IBlockExtraDataProvider
    {
        private readonly ITransactionResultQueryService _transactionResultQueryService;

        public CrossChainBlockExtraDataProvider(ITransactionResultQueryService transactionResultQueryService)
        {
            _transactionResultQueryService = transactionResultQueryService;
        }

        public async Task FillExtraDataAsync(Block block)
        {
            if (!CrossChainEventHelper.TryGetLogEventInBlock(block, out var interestedLogEvent))
                return;
            try
            {
                foreach (var txId in block.Body.Transactions)
                {
                    var res = await _transactionResultQueryService.GetTransactionResultAsync(txId);
                    
                    var sideChainTransactionsRoot =
                        CrossChainEventHelper.TryGetValidateCrossChainBlockData(res, block, interestedLogEvent, out _);
                    if(sideChainTransactionsRoot == null)
                        continue;
                    if (block.Header.BlockExtraData == null)
                    {
                        block.Header.BlockExtraData = new BlockExtraData();
                    }
                    block.Header.BlockExtraData.SideChainTransactionsRoot = sideChainTransactionsRoot;
                    return;
                }
            }
            catch (Exception)
            {
                // ignored
                // Deserialization/NULL value errors
            }
        }
    }
}