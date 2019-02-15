using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContractExecution.Domain;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class BlockExecutingService : IBlockExecutingService
    {
        private readonly ITransactionExecutingService _executingService;
        private readonly IBlockManager _blockManager;
        private readonly IBlockchainStateManager _blockchainStateManager;

        public BlockExecutingService(ITransactionExecutingService executingService, IBlockManager blockManager,
            IBlockchainStateManager blockchainStateManager)
        {
            _executingService = executingService;
            _blockManager = blockManager;
            _blockchainStateManager = blockchainStateManager;
        }

        public async Task ExecuteBlockAsync(int chainId, Hash blockHash)
        {
            // TODO: If already executed, don't execute again. Maybe check blockStateSet?
            var block = await _blockManager.GetBlockAsync(blockHash);
            var readyTxs = block.Body.TransactionList.ToList();

            // TODO: Use BlockStateSet to calculate merkle tree

            var blockStateSet = new BlockStateSet()
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Header.Height,
                PreviousHash = block.Header.PreviousBlockHash
            };

            var chainContext = new ChainContext()
            {
                ChainId = block.Header.ChainId,
                BlockHash = block.Header.PreviousBlockHash,
                BlockHeight = block.Header.Height - 1
            };
            var returnSets = await _executingService.ExecuteAsync(chainId, chainContext, readyTxs,
                block.Header.Time.ToDateTime(), CancellationToken.None);
            foreach (var returnSet in returnSets)
            {
                foreach (var change in returnSet.StateChanges)
                {
                    blockStateSet.Changes.Add(change.Key, change.Value);
                }
            }

            await _blockchainStateManager.SetBlockStateSetAsync(blockStateSet);
            
            // TODO: Insert deferredTransactions to TxPool
        }
    }
}