using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.EventMessages;
using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using Easy.MessageHub;
using NLog;
using AElf.Common;
using NServiceKit.Common.Extensions;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public class BlockChain : LightChain, IBlockChain
    {    
        private readonly ITransactionManager _transactionManager;
        private readonly ITransactionTraceManager _transactionTraceManager;
        private readonly IStateStore _stateStore;

        private readonly ILogger _logger;

        public BlockChain(Hash chainId, IChainManagerBasic chainManager, IBlockManagerBasic blockManager,
            ITransactionManager transactionManager, ITransactionTraceManager transactionTraceManager,
            IStateStore stateStore, IDataStore dataStore, ILogger logger = null) : base(
            chainId, chainManager, blockManager, dataStore)
        {
            _transactionManager = transactionManager;
            _transactionTraceManager = transactionTraceManager;
            _stateStore = stateStore;
            _logger = logger;
        }

        public IBlock CurrentBlock
        {
            get
            {
                var currentBlockHash = _chainManager.GetCurrentBlockHashAsync(_chainId).Result;
                return _blockManager.GetBlockAsync(currentBlockHash).Result;
            }
        }

        public async Task<bool> HasBlock(Hash blockId)
        {
            var blk = await _blockManager.GetBlockAsync(blockId);
            return blk != null;
        }

        public async Task<bool> IsOnCanonical(Hash blockId)
        {
            throw new NotImplementedException();
        }

        private async Task AddBlockAsync(IBlock block)
        {
            await AddHeaderAsync(block.Header);
            await _blockManager.AddBlockBodyAsync(block.Header.GetHash(), block.Body);
        }

        public async Task AddBlocksAsync(IEnumerable<IBlock> blocks)
        {
            foreach (var block in blocks)
            {
                await AddBlockAsync(block);
            }
        }

        public async Task<IBlock> GetBlockByHashAsync(Hash blockId)
        {
            return await _blockManager.GetBlockAsync(blockId);
        }

        public async Task<IBlock> GetBlockByHeightAsync(ulong height)
        {
            var header = await GetHeaderByHeightAsync(height);
            if (header == null)
            {
                return null;
            }

            return await GetBlockByHashAsync(header.GetHash());
        }

        public async Task<List<Transaction>> RollbackOneBlock()
        {
            var currentHeight = await GetCurrentBlockHeightAsync();
            return await RollbackToHeight(currentHeight);
        }

        public async Task<List<Transaction>> RollbackToHeight(ulong height)
        {
            _logger?.Trace("Will rollback to height: " + height);

            var currentHash = await GetCurrentBlockHashAsync();
            var currentHeight = ((BlockHeader) await GetHeaderByHashAsync(currentHash)).Index;

            var txs = new List<Transaction>();
            if (currentHeight <= height)
            {
                return txs;
            }

            for (var i = currentHeight; i > height; i--)
            {
                var block = await GetBlockByHeightAsync(i);
                var body = block.Body;
                foreach (var txId in body.Transactions)
                {
                    var tx = await _transactionManager.GetTransaction(txId);
                    txs.Add(tx);
                }

                var h = GetHeightHash(i).OfType(HashType.CanonicalHash);
                await _dataStore.RemoveAsync<Hash>(h);
                await RollbackStateForBlock(block);
            }

            var hash = await GetCanonicalHashAsync(height);

            await _chainManager.UpdateCurrentBlockHashAsync(_chainId, hash);
            MessageHub.Instance.Publish(
                new RevertedToBlockHeader(((BlockHeader) await GetHeaderByHashAsync(currentHash))));
            return txs;

        }

        private async Task RollbackStateForBlock(IBlock block)
        {
            var txIds = block.Body.Transactions;
            var disambiguationHash = HashHelpers.GetDisambiguationHash(block.Header.Index, Address.FromRawBytes(block.Header.P.ToByteArray()));
            await RollbackStateForTransactions(txIds, disambiguationHash);
        }

        public async Task RollbackStateForTransactions(IEnumerable<Hash> txIds, Hash disambiguationHash)
        {
            var origValues = new Dictionary<StatePath, byte[]>();
            foreach (var txId in txIds.Reverse())
            {
                var trace = await _transactionTraceManager.GetTransactionTraceAsync(txId, disambiguationHash);
                foreach (var kv in trace.StateChanges)
                {
                    origValues.Add(kv.StatePath, kv.StateValue.OriginalValue.ToByteArray());
                }
            }

            await _stateStore.PipelineSetDataAsync(origValues);
        }
    }
}