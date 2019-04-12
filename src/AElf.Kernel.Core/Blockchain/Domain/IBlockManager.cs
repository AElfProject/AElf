using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Blockchain.Domain
{
    public interface IBlockManager
    {
        Task AddBlockHeaderAsync(BlockHeader header);
        Task AddBlockBodyAsync(Hash blockHash, BlockBody blockBody);
        Task<Block> GetBlockAsync(Hash blockHash);
        Task<BlockHeader> GetBlockHeaderAsync(Hash blockHash);
        Task RemoveBlockAsync(Hash blockHash);
    }
    
    public class BlockManager : IBlockManager
    {
        private readonly IBlockchainStore<BlockHeader> _blockHeaderStore;
        private readonly IBlockchainStore<BlockBody> _blockBodyStore;
        public ILogger<BlockManager> Logger {get;set;}

        public BlockManager(IBlockchainStore<BlockHeader> blockHeaderStore, IBlockchainStore<BlockBody> blockBodyStore)
        {
            Logger = NullLogger<BlockManager>.Instance;
            _blockHeaderStore = blockHeaderStore;
            _blockBodyStore = blockBodyStore;
        }
        
        public async Task AddBlockHeaderAsync(BlockHeader header)
        {
            await _blockHeaderStore.SetAsync(header.GetHash().ToStorageKey(), header);
        }

        public async Task AddBlockBodyAsync(Hash blockHash, BlockBody blockBody)
        {
            blockBody.TransactionList.Clear();
            await _blockBodyStore.SetAsync(blockHash.Clone().ToStorageKey(), blockBody);
        }
        
        public async Task<Block> GetBlockAsync(Hash blockHash)
        {
            if (blockHash == null)
            {
                return null;
            }

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
                Logger.LogError(e, $"Error while getting block {blockHash.ToHex()}.");
                return null;
            }
        }

        public async Task<BlockHeader> GetBlockHeaderAsync(Hash blockHash)
        {
            return await _blockHeaderStore.GetAsync(blockHash.ToStorageKey());
        }

        private async Task<BlockBody> GetBlockBodyAsync(Hash bodyHash)
        {
            return await _blockBodyStore.GetAsync(bodyHash.ToStorageKey());
        }

        public async Task RemoveBlockAsync(Hash blockHash)
        {
            var blockKey = blockHash.ToStorageKey();
            await _blockHeaderStore.RemoveAsync(blockKey);
            await _blockBodyStore.RemoveAsync(blockKey);
        }
    }
}