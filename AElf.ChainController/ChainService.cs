using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
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

        private readonly Dictionary<Hash, BlockChain> _blockChains = new Dictionary<Hash, BlockChain>();

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
            // To prevent some weird situations.
            if (chainId == Hash.Default && _blockChains.Any())
            {
                return _blockChains.First().Value;
            }
            
            if (_blockChains.TryGetValue(chainId, out var blockChain))
            {
                return blockChain;
            }
            
            blockChain = new BlockChain(chainId, _chainManager, _blockManager, _transactionManager, _dataStore);
            _blockChains.Add(chainId, blockChain);
            return blockChain;
        }

        public ILightChain GetLightChain(Hash chainId)
        {
            return new LightChain(chainId, _chainManager, _blockManager, _dataStore);
        }
    }
}