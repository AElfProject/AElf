using System.Collections.Concurrent;
using System.Linq;
using AElf.Common;
using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using Akka.Dispatch;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel
{
    public class ChainService : IChainService, ISingletonDependency
    {
        private readonly IChainManager _chainManager;
        private readonly IBlockManager _blockManager;
        private readonly ITransactionManager _transactionManager;
        private readonly ITransactionTraceManager _transactionTraceManager;
        private readonly IStateManager _stateManager;

        private readonly ConcurrentDictionary<int, BlockChain> _blockchains = new ConcurrentDictionary<int, BlockChain>();

        public ChainService(IChainManager chainManager, IBlockManager blockManager,
            ITransactionManager transactionManager, ITransactionTraceManager transactionTraceManager, 
            IStateManager stateManager)
        {
            _chainManager = chainManager;
            _blockManager = blockManager;
            _transactionManager = transactionManager;
            _transactionTraceManager = transactionTraceManager;
            _stateManager = stateManager;
        }

        public IBlockChain GetBlockChain(int chainId)
        {
            
            if (_blockchains.TryGetValue(chainId, out var blockChain))
            {
                return blockChain;
            }

            blockChain = new BlockChain(chainId, _chainManager, _blockManager, _transactionManager,
                _transactionTraceManager, _stateManager);
            _blockchains.TryAdd(chainId, blockChain);
            return blockChain;
        }

        public ILightChain GetLightChain(int chainId)
        {
            return new LightChain(chainId, _chainManager, _blockManager);
        }
    }
}