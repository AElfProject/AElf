using System;
using System.Threading.Tasks;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class ChainCreationService: IChainCreationService
    {
        private readonly IChainManager _chainManager;
        private readonly ITransactionManager _transactionManager;

        public ChainCreationService(IChainManager chainManager, ITransactionManager transactionManager)
        {
            _chainManager = chainManager;
            _transactionManager = transactionManager;
        }


        public async Task CreateNewChainAsync(Hash chainId,Type smartContract)
        {
            var chain = await _chainManager.AddChainAsync(chainId);
            var builder= new GenesisBlockBuilder();
            builder.Build(smartContract);
            
            foreach (var tx in builder.Txs)
            {
                await _transactionManager.AddTransactionAsync(tx);
            }
            await _chainManager.AppendBlockToChainAsync(chain, builder.Block);
        }
    }
}