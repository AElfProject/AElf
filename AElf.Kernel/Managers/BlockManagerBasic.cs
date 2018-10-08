using System;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Kernel.Storages;
using Google.Protobuf.WellKnownTypes;
using NLog;

namespace AElf.Kernel.Managers
{
    public class BlockManagerBasic : IBlockManagerBasic
    {
        private readonly IDataStore _dataStore;

        private readonly ILogger _logger;

        public BlockManagerBasic(IDataStore dataStore, ILogger logger)
        {
            _dataStore = dataStore;
            _logger = logger;
        }

        public async Task<IBlock> AddBlockAsync(IBlock block)
        {
            await _dataStore.InsertAsync(block.GetHash().OfType(HashType.BlockHeaderHash), block.Header);
            await _dataStore.InsertAsync(block.GetHash().OfType(HashType.BlockBodyHash), block.Body);

            return block;
        }

        public async Task AddBlockBodyAsync(Hash blockHash, BlockBody blockBody)
        {
            await _dataStore.InsertAsync(blockHash.Clone().OfType(HashType.BlockBodyHash), blockBody);
        }

        public async Task<BlockHeader> GetBlockHeaderAsync(Hash blockHash)
        {
            return await _dataStore.GetAsync<BlockHeader>(blockHash.Clone().OfType(HashType.BlockHeaderHash));
        }

        public async Task<BlockBody> GetBlockBodyAsync(Hash bodyHash)
        {
            return await _dataStore.GetAsync<BlockBody>(bodyHash.Clone().OfType(HashType.BlockBodyHash));
        }

        public async Task<BlockHeader> AddBlockHeaderAsync(BlockHeader header)
        {
            await _dataStore.InsertAsync(header.GetHash().OfType(HashType.BlockHeaderHash), header);
            return header;
        }

        public async Task<Block> GetBlockAsync(Hash blockHash)
        {
            return new Block
            {
                Header = await _dataStore.GetAsync<BlockHeader>(blockHash.Clone().OfType(HashType.BlockHeaderHash)),
                Body = await _dataStore.GetAsync<BlockBody>(blockHash.Clone().OfType(HashType.BlockBodyHash))
            };
        }

        /// <summary>
        /// Bind child chain height with parent height who indexed it.  
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="childHeight"></param>
        /// <param name="parentHeight"></param>
        /// <returns></returns>
        public async Task BindParentChainHeight(Hash chainId, ulong childHeight, ulong parentHeight)
        {
            var key = DataPath.CalculatePointerForParentChainHeightByChildChainHeight(chainId, childHeight);
            await _dataStore.InsertAsync(key, new UInt64Value {Value = parentHeight});
        }

        /// <summary>
        /// Get the parent chain block height indexing the child chain <param name="childHeight"/>.
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="childHeight"></param>
        /// <returns></returns>
        public async Task<ulong> GetBoundParentChainHeight(Hash chainId, ulong childHeight)
        {
            var key = DataPath.CalculatePointerForParentChainHeightByChildChainHeight(chainId, childHeight);
            return (await _dataStore.GetAsync<UInt64Value>(key))?.Value ?? 0;
        }

        public async Task<Block> GetNextBlockOf(Hash chainId, Hash blockHash)
        {
            var nextBlockHeight = (await GetBlockHeaderAsync(blockHash)).Index + 1;
            var nextBlockHash = await _dataStore.GetAsync<Hash>(
                DataPath.CalculatePointerForGettingBlockHashByHeight(chainId, nextBlockHeight));
            return await GetBlockAsync(nextBlockHash); 
        }
        
        public async Task<Block> GetBlockByHeight(Hash chainId, ulong height)
        {
            _logger?.Trace($"Trying to get block by height {height}");

            var key = DataPath.CalculatePointerForGettingBlockHashByHeight(chainId, height);
            if (key == null)
            {
                _logger?.Error($"Invalid block height - {height}");
                return null;
            }
            
            var blockHash = await _dataStore.GetAsync<Hash>(key);
            
            var blockHeader = await _dataStore.GetAsync<BlockHeader>(blockHash.OfType(HashType.BlockHeaderHash));
            var blockBody = await _dataStore.GetAsync<BlockBody>(blockHash.OfType(HashType.BlockBodyHash));
            return new Block
            {
                Header = blockHeader,
                Body = blockBody
            };
        }
    }
}