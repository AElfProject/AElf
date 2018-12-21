using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Manager.Interfaces;
using AElf.Kernel.Storage.Interfaces;
using NLog;

namespace AElf.Kernel.Manager.Managers
{
    public class BlockManager : IBlockManager
    {
        private readonly IBlockHeaderStore _blockHeaderStore;
        private readonly IBlockBodyStore _blockBodyStore;
        private readonly ILogger _logger;

        public BlockManager(IBlockHeaderStore blockHeaderStore, IBlockBodyStore blockBodyStore)
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
            await _blockHeaderStore.SetAsync(header.GetHash().DumpHex(), header);
        }

        public async Task AddBlockBodyAsync(Hash blockHash, BlockBody blockBody)
        {
            await _blockBodyStore.SetAsync(blockHash.Clone().DumpHex(), blockBody);
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
                _logger.Error(e, $"Error while getting block {blockHash.DumpHex()}.");
                return null;
            }
        }

        public async Task<BlockHeader> GetBlockHeaderAsync(Hash blockHash)
        {
            return await _blockHeaderStore.GetAsync<BlockHeader>(blockHash.DumpHex());
        }

        public async Task<BlockBody> GetBlockBodyAsync(Hash bodyHash)
        {
            return await _blockBodyStore.GetAsync<BlockBody>(bodyHash.DumpHex());
        }
    }
}