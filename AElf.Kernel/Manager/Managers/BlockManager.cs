using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Manager.Interfaces;
using AElf.Kernel.Storage;
using NLog;

namespace AElf.Kernel.Manager.Managers
{
    public class BlockManager : IBlockManager
    {
        private readonly IKeyValueStore _blockHeaderStore;
        private readonly IKeyValueStore _blockBodyStore;
        private readonly ILogger _logger;

        public BlockManager(BlockHeaderStore blockHeaderStore, BlockBodyStore blockBodyStore)
        {
            _blockHeaderStore = blockHeaderStore;
            _blockBodyStore = blockBodyStore;
            _logger = LogManager.GetLogger(nameof(BlockManager));
        }

        public async Task AddBlockAsync(IBlock block)
        {
            await AddBlockHeaderAsync(block.Header);
            await AddBlockBodyAsync(block.GetHash(), block.Body);
        }
        
        public async Task AddBlockHeaderAsync(BlockHeader header)
        {
            await _blockHeaderStore.SetAsync(header.GetHash().ToHex(), header);
        }

        public async Task AddBlockBodyAsync(Hash blockHash, BlockBody blockBody)
        {
            blockBody.TransactionList.Clear();
            await _blockBodyStore.SetAsync(blockHash.Clone().ToHex(), blockBody);
        }
        
        public async Task<Block> GetBlockAsync(Hash blockHash)
        {
            try
            {
                var header = await GetBlockHeaderAsync(blockHash);
                var bb = await GetBlockBodyAsync(blockHash);

                if (header == null || bb == null)
                    return null;

                return new Block { Header = header, Body = bb };
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Error while getting block {blockHash.ToHex()}.");
                return null;
            }
        }

        public async Task<BlockHeader> GetBlockHeaderAsync(Hash blockHash)
        {
            return await _blockHeaderStore.GetAsync<BlockHeader>(blockHash.ToHex());
        }

        public async Task<BlockBody> GetBlockBodyAsync(Hash bodyHash)
        {
            return await _blockBodyStore.GetAsync<BlockBody>(bodyHash.ToHex());
        }
    }
}