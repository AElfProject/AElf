using System;
using System.Threading.Tasks;
using AElf.Kernel.Storages;
using AElf.Kernel.Types;
using Google.Protobuf.WellKnownTypes;
using NLog;

namespace AElf.Kernel.Managers
{
    public class BlockManager : IBlockManager
    {
        private readonly IBlockHeaderStore _blockHeaderStore;

        private readonly IBlockBodyStore _blockBodyStore;

        private readonly IDataStore _dataStore;

        private readonly ILogger _logger;

        public BlockManager(IBlockHeaderStore blockHeaderStore, IBlockBodyStore blockBodyStore,
            IDataStore dataStore, ILogger logger)
        {
            _blockHeaderStore = blockHeaderStore;
            _blockBodyStore = blockBodyStore;
            _dataStore = dataStore;
            _logger = logger;
        }

        public async Task<IBlock> AddBlockAsync(IBlock block)
        {
            await _blockHeaderStore.InsertAsync(block.Header);
            await _blockBodyStore.InsertAsync(block.Body.GetHash(), block.Body);

            return block;
        }

        public async Task<BlockHeader> GetBlockHeaderAsync(Hash blockHash)
        {
            return await _blockHeaderStore.GetAsync(blockHash);
        }

        public async Task<BlockHeader> AddBlockHeaderAsync(BlockHeader header)
        {
            return await _blockHeaderStore.InsertAsync(header);
        }

        public async Task<Block> GetBlockAsync(Hash blockHash)
        {
            var header = await _blockHeaderStore.GetAsync(blockHash);
            var body = await _blockBodyStore.GetAsync(header.GetHash().CalculateHashWith(header.MerkleTreeRootOfTransactions));
            return new Block
            {
                Header = header,
                Body = body
            };
        }
        
        public async Task<Block> GetNextBlockOf(Hash chainId, Hash blockHash)
        {
            var nextBlockHeight = (await GetBlockAsync(blockHash)).Header.Index + 1;
            var nextBlockHashBytes = await _dataStore.GetDataAsync(
                ResourcePath.CalculatePointerForGettingBlockHashByHeight(chainId, nextBlockHeight));
            var nextBlockHash = Hash.Parser.ParseFrom(nextBlockHashBytes);
            return await GetBlockAsync(nextBlockHash);
        }
        
        public async Task<Block> GetBlockByHeight(Hash chainId, ulong height)
        {
            _logger?.Trace($"{GetLogPrefix()}Trying to get block by height {height}");

            var keyQuote = await _dataStore.GetDataAsync(
                ResourcePath.CalculatePointerForGettingBlockHashByHeight(chainId, height));
            if (keyQuote == null)
            {
                _logger?.Error($"{GetLogPrefix()}Invalid block height - {height}");
                return null;
            }
            var key = Hash.Parser.ParseFrom(keyQuote);

            var blockHeader = await _blockHeaderStore.GetAsync(key);
            var blockBody = await _blockBodyStore.GetAsync(blockHeader.GetHash().CalculateHashWith(blockHeader.MerkleTreeRootOfTransactions));
            return new Block
            {
                Header = blockHeader,
                Body = blockBody
            };
        }

        /// <summary>
        /// Use "nameof" in case of chaging this class name
        /// </summary>
        /// <returns></returns>
        private static string GetLogPrefix()
        {
            return $"[{DateTime.UtcNow.ToLocalTime(): HH:mm:ss} - {nameof(BlockManager)}] ";
        }
    }
}