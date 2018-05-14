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


        public async Task<IChain> CreateNewChainAsync(Hash chainId,Type smartContract)
        {
            var chain = await _chainManager.AddChainAsync(chainId);
            var builder= new GenesisBlockBuilder();
            builder.Build(smartContract);
            
            foreach (var tx in builder.Txs)
            {
                await _transactionManager.AddTransactionAsync(tx);
            }

            await _blockManager.AddBlockAsync(builder.Block);
            await _chainManager.AppendBlockToChainAsync(chain, builder.Block);

            return chain;
        }
    }
}