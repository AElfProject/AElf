using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Domain;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Blockchain.Application
{
    public interface ITransactionBlockIndexService
    {
        Task<BlockIndex> GetTransactionBlockIndexAsync(Hash txId);
        Task UpdateTransactionBlockIndexAsync(Hash txId, BlockIndex blockIndex);
    }
    
    public class TransactionBlockIndexService : ITransactionBlockIndexService, ITransientDependency
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionBlockIndexManager _transactionBlockIndexManager;

        public TransactionBlockIndexService(IBlockchainService blockchainService, 
            ITransactionBlockIndexManager transactionBlockIndexManager)
        {
            _blockchainService = blockchainService;
            _transactionBlockIndexManager = transactionBlockIndexManager;
        }

        public async Task<BlockIndex> GetTransactionBlockIndexAsync(Hash txId)
        {
            var transactionBlockIndex =
                await _transactionBlockIndexManager.GetTransactionBlockIndexAsync(txId);

            if (transactionBlockIndex == null)
                return null;
            
            var chain = await _blockchainService.GetChainAsync();

            var reversedBlockIndexList = transactionBlockIndex.PreviousExecutionBlockIndexList
                .Concat(new[] {transactionBlockIndex.BlockIndex}).Reverse().ToList();
            
            foreach (var blockIndex in reversedBlockIndexList)
            {
                var blockHashInBestChain =
                    await _blockchainService.GetBlockHashByHeightAsync(chain, blockIndex.BlockHeight, chain.BestChainHash);

                // check whether it is on best chain or a fork branch
                if (blockIndex.BlockHash == blockHashInBestChain)
                    // If TransactionBlockIndex exists, then read the result via TransactionBlockIndex
                    return blockIndex;
            }

            return null;
        }
        
        public async Task UpdateTransactionBlockIndexAsync(Hash txId, BlockIndex blockIndex)
        {
            var preTransactionBlockIndex =
                await _transactionBlockIndexManager.GetTransactionBlockIndexAsync(txId);

            var transactionBlockIndex = new TransactionBlockIndex
            {
                BlockIndex = blockIndex
            };
                    
            if (preTransactionBlockIndex != null)
            {
                transactionBlockIndex.PreviousExecutionBlockIndexList.Add(preTransactionBlockIndex
                    .PreviousExecutionBlockIndexList);
                transactionBlockIndex.PreviousExecutionBlockIndexList.Add(preTransactionBlockIndex.BlockIndex);
            }

            await _transactionBlockIndexManager.SetTransactionBlockIndexAsync(txId, transactionBlockIndex);
        }
    }
}