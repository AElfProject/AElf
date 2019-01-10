using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.EventMessages;
using Easy.MessageHub;
using AElf.Common;
using AElf.Kernel.Managers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public class BlockChain : LightChain, IBlockChain
    {
        private readonly ITransactionManager _transactionManager;
        private readonly ITransactionTraceManager _transactionTraceManager;
        private readonly IStateManager _stateManager;
        public new ILogger<BlockChain> Logger { get; set; }


        public BlockChain(Hash chainId, IChainManager chainManager, IBlockManager blockManager,
            ITransactionManager transactionManager, ITransactionTraceManager transactionTraceManager,
            IStateManager stateManager) : base(
            chainId, chainManager, blockManager)
        {
            _transactionManager = transactionManager;
            _transactionTraceManager = transactionTraceManager;
            _stateManager = stateManager;

            Logger = NullLogger<BlockChain>.Instance;
        }

        public async Task<bool> HasBlock(Hash blockId)
        {
            var blk = await _blockManager.GetBlockAsync(blockId);
            return blk != null;
        }

        public new Task<bool> IsOnCanonical(Hash blockId)
        {
            throw new NotImplementedException();
        }

        private async Task AddBlockAsync(IBlock block)
        {
            await AddHeaderAsync(block.Header);
            // TODO: This will be problematic if the block is used somewhere else after this method
            //block.Body.TransactionList.Clear();
            await _blockManager.AddBlockBodyAsync(block.Header.GetHash(), block.Body);
        }

        public async Task AddBlocksAsync(IEnumerable<IBlock> blocks)
        {
            foreach (var block in blocks)
            {
                await AddBlockAsync(block);
            }
        }

        public async Task<IBlock> GetBlockByHashAsync(Hash blockId, bool withTransaction = false)
        {
            var blk = await _blockManager.GetBlockAsync(blockId);
            if (!withTransaction)
            {
                return blk;
            }

            blk.Body.TransactionList.Clear();
            foreach (var txHash in blk.Body.Transactions)
            {
                var t = await _transactionManager.GetTransaction(txHash);
                blk.Body.TransactionList.Add(t);
            }

            return blk;
        }

        public async Task<IBlock> GetBlockByHeightAsync(ulong height, bool withTransaction = false)
        {
            var header = await GetHeaderByHeightAsync(height);
            if (header == null)
            {
                return null;
            }

            return await GetBlockByHashAsync(header.GetHash(), withTransaction);
        }

        public async Task<List<Transaction>> RollbackToHeight(ulong height)
        {
            var txs = new List<Transaction>();

            MessageHub.Instance.Publish(new RollBackStateChanged(true));

            Logger.LogTrace("Will rollback to " + height);

            var currentHash = await GetCurrentBlockHashAsync();
            var currentHeight = ((BlockHeader) await GetHeaderByHashAsync(currentHash)).Index;

            if (currentHeight <= height)
            {
                return txs;
            }

            var blocks = new List<Block>();
            for (var i = currentHeight; i > height; i--)
            {
                var block = await GetBlockByHeightAsync(i);
                var body = block.Body;
                foreach (var txId in body.Transactions)
                {
                    var tx = await _transactionManager.GetTransaction(txId);
                    txs.Add(tx);
                }

                await _chainManager.RemoveCanonical(_chainId, i);
                await RollbackSideChainInfo(block);
                await RollbackStateForBlock(block);
                blocks.Add((Block) block);
            }

            blocks.Reverse();

            var hash = await GetCanonicalHashAsync(height);

            await _chainManager.UpdateCurrentBlockHashAsync(_chainId, hash);

            MessageHub.Instance.Publish(new BranchRolledBack(blocks));
            Logger.LogTrace("Finished rollback to " + height);
            MessageHub.Instance.Publish(new RollBackStateChanged(false));

            return txs;
        }

        private async Task RollbackSideChainInfo(IBlock block)
        {
            foreach (var info in block.Body.IndexedInfo)
            {
                await _chainManager.UpdateCurrentBlockHeightAsync(info.ChainId,
                    info.Height > GlobalConfig.GenesisBlockHeight ? info.Height - 1 : 0);
            }
        }

        private async Task RollbackStateForBlock(IBlock block)
        {
            var txIds = block.Body.Transactions;
            var disambiguationHash =
                HashHelpers.GetDisambiguationHash(block.Header.Index, Hash.FromRawBytes(block.Header.P.ToByteArray()));
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
                    origValues[kv.StatePath] = kv.StateValue.OriginalValue.ToByteArray();
                }
            }

            await _stateManager.PipelineSetAsync(origValues);
        }
    }
}