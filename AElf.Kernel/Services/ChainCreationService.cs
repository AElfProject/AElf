using System;
using System.Threading.Tasks;
using AElf.Kernel.Managers;

namespace AElf.Kernel.Services
{
    public class ChainCreationService: IChainCreationService
    {
        private readonly IChainManager _chainManager;
        private readonly ITransactionManager _transactionManager;
        private readonly IBlockManager _blockManager;

        public ChainCreationService(IChainManager chainManager, ITransactionManager transactionManager, IBlockManager blockManager)
        {
            _chainManager = chainManager;
            _transactionManager = transactionManager;
            _blockManager = blockManager;
        }


        public async Task<IChain> CreateNewChainAsync(Hash chainId, Type smartContract)
        {
            var builder= new GenesisBlockBuilder();
            builder.Build(smartContract);
            
            foreach (var tx in builder.Txs)
            {
                builder.Block.Body.Transactions.Add(tx.GetHash());
                await _transactionManager.AddTransactionAsync(tx);
            }

            
            // add block to storage
            await _blockManager.AddBlockAsync(builder.Block);
            
            // set height and lastBlockHash for a chain
            await _chainManager.SetChainCurrentHeight(chainId, 0);
            await _chainManager.SetChainLastBlockHash(chainId, builder.Block.GetHash());
            var chain = await _chainManager.AddChainAsync(chainId, builder.Block.GetHash());
            await _chainManager.AppendBlockToChainAsync(chainId, builder.Block);
            return chain;
        }
    }
}