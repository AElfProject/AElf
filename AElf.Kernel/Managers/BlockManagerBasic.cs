using System;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Kernel.Storages;
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