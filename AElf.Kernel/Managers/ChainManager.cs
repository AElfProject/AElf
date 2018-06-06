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

        public ChainManager(IChainStore chainStore, IDataStore dataStore)
        {
            _chainStore = chainStore;
            _dataStore = dataStore;
        }

        public async Task AppendBlockToChainAsync(Hash chainId, IBlock block)
        {
            if(block.Header == null)
                throw new InvalidDataException("Invalid block");

            await AppendBlockHeaderAsync(chainId, block.Header);
        }

        public async Task AppendBlockHeaderAsync(Hash chainId, BlockHeader header)
        {
            if (await _chainStore.GetAsync(chainId) == null)
                throw new KeyNotFoundException("Not existed Chain");
            
            var height = await GetChainCurrentHeightAsync(chainId);
            var lastBlockHash = await GetChainLastBlockHashAsync(chainId);
            // chain height should not be 0 when appending a new block
            if (height == 0)
            {
                // empty chain
                await SetChainCurrentHeightAsync(chainId, 1);
                await SetChainLastBlockHashAsync(chainId, header.GetHash());
            }
            else if ( lastBlockHash != header.PreviousHash)
            {
                throw new InvalidDataException("Invalid block");
                //Block is not connected
            }
            header.Index = height;
            await SetChainCurrentHeightAsync(chainId, height + 1);
            await SetChainLastBlockHashAsync(chainId, header.GetHash());
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
        public async Task<ulong> GetChainCurrentHeightAsync(Hash chainId)
        {
            var key = Path.CalculatePointerForCurrentBlockHeight(chainId);
            var heightBytes = await _dataStore.GetDataAsync(key);
            return heightBytes?.ToUInt64() ?? 0;
        }

        /// <inheritdoc/>
        public async Task SetChainCurrentHeightAsync(Hash chainId, ulong height)
        {
            var key = Path.CalculatePointerForCurrentBlockHeight(chainId);
            await _dataStore.SetDataAsync(key, height.ToBytes());
        }

        /// <inheritdoc/>
        public async Task<Hash> GetChainLastBlockHashAsync(Hash chainId)
        {
            var key = Path.CalculatePointerForLastBlockHash(chainId);
            return await _dataStore.GetDataAsync(key);
        }

        /// <inheritdoc/>
        public async Task SetChainLastBlockHashAsync(Hash chainId, Hash blockHash)
        {
            var key = Path.CalculatePointerForLastBlockHash(chainId);
            await _dataStore.SetDataAsync(key, blockHash.GetHashBytes());
        }
        
    }
}