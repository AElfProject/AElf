using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using ServiceStack;

namespace AElf.ChainController
{
    public class ChainService : IChainService
    {
        private readonly IChainManagerBasic _chainManager;
        private readonly IBlockManagerBasic _blockManager;
        private readonly ITransactionManager _transactionManager;
        private readonly IDataStore _dataStore;

        public ChainService(IChainManagerBasic chainManager, IBlockManagerBasic blockManager,
            ITransactionManager transactionManager, IDataStore dataStore)
        {
            _chainManager = chainManager;
            _blockManager = blockManager;
            _transactionManager = transactionManager;
            _dataStore = dataStore;
        }

        public IBlockChain GetBlockChain(Hash chainId)
        {
            return new BlockChain(chainId, _chainManager, _blockManager, _transactionManager, _dataStore);
        }

        public ILightChain GetLightChain(Hash chainId)
        {
            return new LightChain(chainId, _chainManager, _blockManager, _dataStore);
        }
    }
}