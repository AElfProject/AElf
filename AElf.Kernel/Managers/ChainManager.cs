using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.Storages;

namespace AElf.Kernel.Managers
{
    public class ChainManager : IChainManager
    {
        private readonly IChainStore _chainStore;
        private readonly IDataStore _dataStore;

        private const string Height = "Height";
        private const string LastBlockHash = "LastBlockHash";
        
        public ChainManager(IChainStore chainStore, IDataStore dataStore)
        {
            _chainStore = chainStore;
            _dataStore = dataStore;
        }

        public async Task AppendBlockToChainAsync(Hash chainId, IBlock block)
        {
            if(block.Header == null)
                throw new InvalidDataException("Invalid block");

            await AppednBlockHeaderAsync(chainId, block.Header);
        }

        public async Task AppednBlockHeaderAsync(Hash chainId, IBlockHeader header)
        {
            if (await _chainStore.GetAsync(chainId) == null)
                throw new KeyNotFoundException("Not existed Chain");
            
            var height = await GetChainCurrentHeight(chainId);
            var lastBlockHash = await GetChainLastBlockHash(chainId);
            // chain height should not be 0 when appending a new block
            if (height == 0)
            {
                // empty chain
                await SetChainCurrentHeight(chainId, 1);
                await SetChainLastBlockHash(chainId, header.GetHash());
            }
            else if ( lastBlockHash != header.PreviousHash)
            {
                throw new InvalidDataException("Invalid block");
                //Block is not connected
            }
            
            await SetChainCurrentHeight(chainId, height + 1);
            await SetChainLastBlockHash(chainId, header.GetHash());
        }


        public Task<IChain> GetChainAsync(Hash id)
        {
            return _chainStore.GetAsync(id);
        }

        public Task<IChain> AddChainAsync(Hash chainId, Hash genesisBlockHash)
        {
            return _chainStore.InsertAsync(new Chain(chainId, genesisBlockHash));
        }
        
        /// <inheritdoc/>
        public async Task<ulong> GetChainCurrentHeight(Hash chainId)
        {
            var key = CalculateKeyForCurrentHeight(chainId);
            return (await _dataStore.GetDataAsync(key)).ToInt64();
        }

        /// <inheritdoc/>
        public async Task SetChainCurrentHeight(Hash chainId, ulong height)
        {
            var key = CalculateKeyForCurrentHeight(chainId);
            await _dataStore.SetDataAsync(key, height.ToBytes());
        }

        /// <inheritdoc/>
        public async Task<Hash> GetChainLastBlockHash(Hash chainId)
        {
            var key = CalculateKeyForLastBlockHash(chainId);
            return await _dataStore.GetDataAsync(key);
        }

        /// <inheritdoc/>
        public async Task SetChainLastBlockHash(Hash chainId, Hash blockHash)
        {
            var key = CalculateKeyForCurrentHeight(chainId);
            await _dataStore.SetDataAsync(key, blockHash.GetHashBytes());
        }

        /// <summary>
        /// calculate key for CurrentHeight storage
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        private Hash CalculateKeyForCurrentHeight(Hash chainId)
        {
            return chainId.CalculateHashWith((Hash)Height.CalculateHash());
        }
        
        
        /// <summary>
        /// calculate key for LastBlockHash storage
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        private Hash CalculateKeyForLastBlockHash(Hash chainId)
        {
            return chainId.CalculateHashWith((Hash)LastBlockHash.CalculateHash());
        }
    }
}