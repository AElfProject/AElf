using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration;
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
        private readonly IBlockSet _blockSet;

        private readonly Dictionary<Hash, BlockChain> _blockchains = new Dictionary<Hash, BlockChain>();

        public ChainService(IChainManagerBasic chainManager, IBlockManagerBasic blockManager,
            ITransactionManager transactionManager, IDataStore dataStore, IBlockSet blockSet)
        {
            _chainManager = chainManager;
            _blockManager = blockManager;
            _transactionManager = transactionManager;
            _dataStore = dataStore;
            _blockSet = blockSet;
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
            
            blockChain = new BlockChain(chainId, _chainManager, _blockManager, _transactionManager, _dataStore);
            _blockchains.Add(chainId, blockChain);
            return blockChain;
        }

        public ILightChain GetLightChain(Hash chainId)
        {
            return new LightChain(chainId, _chainManager, _blockManager, _dataStore);
        }

        public bool IsBlockReceived(Hash blockHash, ulong height)
        {
            return _blockSet.IsBlockReceived(blockHash, height);
        }

        public IBlock GetBlockByHash(Hash blockHash)
        {
            return _blockSet.GetBlockByHash(blockHash) ?? GetBlockChain(Hash.LoadHex(NodeConfig.Instance.ChainId))
                       .GetBlockByHashAsync(blockHash).Result;
        }

        public List<IBlock> GetBlockByHeight(ulong height)
        {
            return _blockSet.GetBlockByHeight(height);
        }
    }
}