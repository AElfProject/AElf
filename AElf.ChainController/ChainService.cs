using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using Akka.Dispatch;
using ServiceStack;
using Volo.Abp.DependencyInjection;

namespace AElf.ChainController
{
    public class ChainService : IChainService, ISingletonDependency
    {
        private readonly IChainManager _chainManager;
        private readonly IBlockManager _blockManager;
        private readonly ITransactionManager _transactionManager;
        private readonly ITransactionTraceManager _transactionTraceManager;
        private readonly IDataStore _dataStore;
        private readonly IStateStore _stateStore;

        private readonly ConcurrentDictionary<Hash, BlockChain> _blockchains = new ConcurrentDictionary<Hash, BlockChain>();

        public ChainService(IChainManager chainManager, IBlockManager blockManager,
            ITransactionManager transactionManager, ITransactionTraceManager transactionTraceManager, 
            IDataStore dataStore, IStateStore stateStore)
        {
            _chainManager = chainManager;
            _blockManager = blockManager;
            _transactionManager = transactionManager;
            _transactionTraceManager = transactionTraceManager;
            _dataStore = dataStore;
            _stateStore = stateStore;
        }

        public IBlockChain GetBlockChain(Hash chainId)
        {
            // To prevent some weird situations.
            if (chainId == Hash.Default && _blockchains.Any())
            {
                return _blockchains.First().Value;
            }
            
            if (_blockchains.TryGetValue(chainId, out var blockChain))
            {
                return blockChain;
            }

            blockChain = new BlockChain(chainId, _chainManager, _blockManager, _transactionManager,
                _transactionTraceManager, _stateStore, _dataStore);
            _blockchains.TryAdd(chainId, blockChain);
            return blockChain;
        }

        public ILightChain GetLightChain(Hash chainId)
        {
            return new LightChain(chainId, _chainManager, _blockManager, _dataStore);
        }
    }
}