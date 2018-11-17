﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.EventMessages;
using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using Easy.MessageHub;
using NLog;
using AElf.Common;
using AElf.Kernel.Types.Common;
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
        private static bool _doingRollback;
        private static bool _prepareTerminated;
        private static bool _terminated;

        public BlockChain(Hash chainId, IChainManagerBasic chainManager, IBlockManagerBasic blockManager,
            ITransactionManager transactionManager, ITransactionTraceManager transactionTraceManager,
            IStateStore stateStore, IDataStore dataStore) : base(
            chainId, chainManager, blockManager, dataStore)
        {
            _transactionManager = transactionManager;
            _transactionTraceManager = transactionTraceManager;
            _stateStore = stateStore;

            _doingRollback = false;
            _prepareTerminated = false;
            _terminated = false;

            _logger = LogManager.GetLogger(nameof(BlockChain));

            MessageHub.Instance.Subscribe<TerminationSignal>(signal =>
            {
                if (signal.Module == TerminatedModuleEnum.BlockRollback)
                {
                    if (!_doingRollback)
                    {
                        _terminated = true;
                        MessageHub.Instance.Publish(new TerminatedModule(TerminatedModuleEnum.BlockRollback));
                    }
                    else
                    {
                        _prepareTerminated = true;
                    }
                }
            });
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
            // TODO: This will be problematic if the block is used somewhere else after this method
            block.Body.TransactionList.Clear();
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
            return await RollbackToHeight(currentHeight - 1);
        }

        public async Task<List<Transaction>> RollbackToHeight(ulong height)
        {
            try
            {
                _doingRollback = true;
                var txs = new List<Transaction>();

                if (_terminated)
                {
                    return txs;
                }

                MessageHub.Instance.Publish(new RollBackStateChanged(true));

                _logger?.Trace("Will rollback to " + height);

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

                    var h = GetHeightHash(i).OfType(HashType.CanonicalHash);
                    await _dataStore.RemoveAsync<Hash>(h);
                    await RollbackSideChainInfo(block);
                    await RollbackStateForBlock(block);
                    blocks.Add((Block) block);
                }

                blocks.Reverse();

                var hash = await GetCanonicalHashAsync(height);

                await _chainManager.UpdateCurrentBlockHashAsync(_chainId, hash);

                MessageHub.Instance.Publish(new BranchRolledBack(blocks));
                _logger?.Trace("Finished rollback to " + height);
                MessageHub.Instance.Publish(new RollBackStateChanged(false));

                return txs;
            }
            finally
            {
                _doingRollback = false;
                if (_prepareTerminated)
                {
                    _terminated = true;
                    MessageHub.Instance.Publish(new TerminatedModule(TerminatedModuleEnum.BlockRollback));
                }
            }
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
            var disambiguationHash = HashHelpers.GetDisambiguationHash(block.Header.Index,
                Address.FromRawBytes(block.Header.P.ToByteArray()));
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

            await _stateStore.PipelineSetDataAsync(origValues);
        }
    }
}