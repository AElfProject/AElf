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
            await _dataStore.InsertAsync(block.GetHash(), block.Header);
            //await _blockHeaderStore.InsertAsync(block.Header);
            await _dataStore.InsertAsync(block.Body.GetHash(), block.Body);

            return block;
        }

        public async Task AddBlockBodyAsync(Hash blockHash, BlockBody blockBody)
        {
            await _dataStore.InsertAsync(blockHash, blockBody);
        }

        public async Task<BlockHeader> GetBlockHeaderAsync(Hash blockHash)
        {
            return await _dataStore.GetAsync<BlockHeader>(blockHash);
            //return await _blockHeaderStore.GetAsync(blockHash);
        }

        public async Task<BlockBody> GetBlockBodyAsync(Hash bodyHash)
        {
            return await _dataStore.GetAsync<BlockBody>(bodyHash);
        }

        public async Task<BlockHeader> AddBlockHeaderAsync(BlockHeader header)
        {
            await _dataStore.InsertAsync(header.GetHash(), header);
            //await _blockHeaderStore.InsertAsync(header);
            return header;
        }

        public async Task<Block> GetBlockAsync(Hash blockHash)
        {
            var header =  await _dataStore.GetAsync<BlockHeader>(blockHash);
            //var header = await _blockHeaderStore.GetAsync(blockHash);
            var body = await _dataStore.GetAsync<BlockBody>(header.GetHash().CalculateHashWith(header.MerkleTreeRootOfTransactions));
            return new Block
            {
                Header = header,
                Body = body
            };
        }
        
        public async Task<Block> GetNextBlockOf(Hash chainId, Hash blockHash)
        {
            var nextBlockHeight = (await GetBlockAsync(blockHash)).Header.Index + 1;
            var nextBlockHash = await _dataStore.GetAsync<Hash>(
                ResourcePath.CalculatePointerForGettingBlockHashByHeight(chainId, nextBlockHeight));
            return await GetBlockAsync(nextBlockHash);
        }
        
        public async Task<Block> GetBlockByHeight(Hash chainId, ulong height)
        {
            _logger?.Trace($"Trying to get block by height {height}");

            var key = ResourcePath.CalculatePointerForGettingBlockHashByHeight(chainId, height);
            if (key == null)
            {
                _logger?.Error($"Invalid block height - {height}");
                return null;
            }
            
            var blockHash = await _dataStore.GetAsync<Hash>(key);
            
            var blockHeader = await _dataStore.GetAsync<BlockHeader>(blockHash);
            var blockBody = await _dataStore.GetAsync<BlockBody>(blockHeader.GetHash()
                .CalculateHashWith(blockHeader.MerkleTreeRootOfTransactions));
            return new Block
            {
                Header = blockHeader,
                Body = blockBody
            };
        }
    }
}