using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.Storages;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Managers
{
    public class ChainManager : IChainManager
    {
        private readonly IChainStore _chainStore;
        private readonly IDataStore _dataStore;
        private readonly IWorldStateManager _worldStateManager;
        
        private IDataProvider _heightOfBlock;

        public ChainManager(IChainStore chainStore, IDataStore dataStore, IWorldStateManager worldStateManager)
        {
            _chainStore = chainStore;
            _dataStore = dataStore;
            _worldStateManager = worldStateManager;
        }

        public async Task AppendBlockToChainAsync(IBlock block)
        {
            if(block.Header == null)
                throw new InvalidDataException("Invalid block");

            var chainId = block.Header.ChainId;
            await AppednBlockHeaderAsync(block.Header);

            await InitialHeightOfBlock(chainId);
            await _heightOfBlock.SetAsync(new UInt64Value {Value = block.Header.Index}.CalculateHash(), block.GetHash().ToByteArray());
        }

        public async Task AppednBlockHeaderAsync(BlockHeader header)
        {
            var chainId = header.ChainId;
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
            else if ( lastBlockHash != header.PreviousBlockHash)
            {
                throw new InvalidDataException("Invalid block");
                //Block is not connected
            }
            header.Index = height;
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
            var key = Path.CalculatePointerForCurrentBlockHeight(chainId);
            var heightBytes = await _dataStore.GetDataAsync(key);
            return heightBytes?.ToUInt64() ?? 0;
        }

        /// <inheritdoc/>
        public async Task SetChainCurrentHeight(Hash chainId, ulong height)
        {
            var key = Path.CalculatePointerForCurrentBlockHeight(chainId);
            await _dataStore.SetDataAsync(key, height.ToBytes());
        }

        /// <inheritdoc/>
        public async Task<Hash> GetChainLastBlockHash(Hash chainId)
        {
            var key = Path.CalculatePointerForLastBlockHash(chainId);
            return await _dataStore.GetDataAsync(key);
        }

        /// <inheritdoc/>
        public async Task SetChainLastBlockHash(Hash chainId, Hash blockHash)
        {
            var key = Path.CalculatePointerForLastBlockHash(chainId);
            await _dataStore.SetDataAsync(key, blockHash.GetHashBytes());
        }
        
        private async Task InitialHeightOfBlock(Hash chainId)
        {
            await _worldStateManager.OfChain(chainId);
            _heightOfBlock = _worldStateManager.GetAccountDataProvider(Path.CalculatePointerForAccountZero(chainId))
                .GetDataProvider().GetDataProvider("HeightOfBlock");
        }
    }
}