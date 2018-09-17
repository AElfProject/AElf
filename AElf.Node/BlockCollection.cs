using System;
using System.Collections.Generic;
using System.Linq;
using AElf.ChainController;
using AElf.Kernel;
using AElf.Network;
using AElf.Node.Protocol;
using NLog;
using NServiceKit.Common;
using NServiceKit.Text;

namespace AElf.Node
{
    /// <summary>
    /// Provide some functions to operate pending blocks and branched chains.
    /// </summary>
    public class BlockCollection : IBlockCollection
    {
        private bool _isInitialSync = true;

        /// <summary>
        /// To store branched chains.
        /// </summary>
        private readonly HashSet<BranchedChain> _branchedChains = new HashSet<BranchedChain>();

        /// <summary>
        /// To track the latest block height of local chain.
        /// </summary>
        public ulong PendingBlockHeight { get; set; }

        public ulong SyncedHeight => _chainService.GetBlockChain(Globals.CurrentChainId).GetCurrentBlockHeightAsync().Result;

        public List<PendingBlock> PendingBlocks { get; set; } = new List<PendingBlock>();

        public int Count => PendingBlocks.Count;
        public int BranchedChainsCount => _branchedChains.Count;

        private readonly ILogger _logger;
        private readonly IChainService _chainService;

        public BlockCollection(IChainService chainService, ILogger logger = null)
        {
            _chainService = chainService;
            _logger = logger;
        }

        private readonly HashSet<ulong> _initialSyncBlocksIndexes = new HashSet<ulong>();

        public bool ReceivedAllTheBlocksBeforeTargetBlock => (ulong) _initialSyncBlocksIndexes.Count == _targetHeight;

        private ulong _targetHeight = ulong.MaxValue;

        /// <summary>
        /// Basically add the pending block if the block is supposed to be on local chain.
        /// Otherwise add the pending block to branched chains.
        /// </summary>
        /// <param name="pendingBlock"></param>
        public List<PendingBlock> AddPendingBlock(PendingBlock pendingBlock)
        {
            // No need to handle an already exists pending block again.
            if (!PendingBlocks.IsNullOrEmpty() &&
                PendingBlocks.Any(b => new Hash(b.Block.GetHash()) == new Hash(pendingBlock.Block.GetHash())))
            {
                return null;
            }

            if (Globals.IsConsensusGenerator)
            {
                _isInitialSync = false;
            }

            if (_isInitialSync)
            {
                switch (pendingBlock.MsgType)
                {
                    case AElfProtocolMsgType.NewBlock:
                        if (_targetHeight == ulong.MaxValue)
                        {
                            _targetHeight = pendingBlock.Block.Header.Index;
                            AddToPendingBlocks(pendingBlock);
                            PendingBlocks.SortByBlockIndex();
                            _initialSyncBlocksIndexes.Add(_targetHeight);
                            return null;
                        }
                        else
                        {
                            _logger?.Trace("Receive a new block while do initial sync.");
                            return AddBlockToBranchedChains(pendingBlock);
                        }

                    case AElfProtocolMsgType.Block:
                        if (!_initialSyncBlocksIndexes.Contains(pendingBlock.Block.Header.Index) &&
                            !ReceivedAllTheBlocksBeforeTargetBlock)
                        {
                            AddToPendingBlocks(pendingBlock);
                            _initialSyncBlocksIndexes.Add(pendingBlock.Block.Header.Index);
                            if (ReceivedAllTheBlocksBeforeTargetBlock)
                            {
                                _isInitialSync = false;
                            }

                            return null;
                        }
                        else
                        {
                            _logger?.Trace("Receive a forked block while do initial sync.");
                            return AddBlockToBranchedChains(pendingBlock);
                        }
                }

                return null;
            }

            if (!AbleToAdd(pendingBlock))
            {
                _logger?.Trace("Receive an orphan block.");
                return AddBlockToBranchedChains(pendingBlock);
            }
            AddToPendingBlocks(pendingBlock);
            PendingBlockHeight = Math.Max(PendingBlockHeight, pendingBlock.Block.Header.Index);
            return null;
        }

        private bool AbleToAdd(PendingBlock pendingBlock)
        {
            switch (pendingBlock.MsgType)
            {
                case AElfProtocolMsgType.Block:
                    return false;
                case AElfProtocolMsgType.NewBlock:
                    if (PendingBlocks.IsEmpty())
                    {
                        return SyncedHeight + 1 == pendingBlock.Block.Header.Index;
                    }

                    var lastPendingBlock = PendingBlocks.Last().Block;
                    return pendingBlock.Block.Header.Index == lastPendingBlock.Header.Index + 1
                           && pendingBlock.Block.Header.PreviousBlockHash == lastPendingBlock.Header.GetHash();
                default:
                    return false;
            }
        }

        private void AddToPendingBlocks(PendingBlock pendingBlock)
        {
            PendingBlocks.Add(pendingBlock);
            PendingBlocks.SortByBlockIndex();
        }

        /// <summary>
        /// Add the pending block to branched chain after removing.
        /// </summary>
        /// <param name="pendingBlock"></param>
        public void RemovePendingBlock(PendingBlock pendingBlock)
        {
            PendingBlocks.Remove(pendingBlock);

            if (PendingBlocks.IsEmpty() && BranchedChainsCount > 0)
            {
                PendingBlocks = _branchedChains.First(c => c.CanCheckout(PendingBlockHeight, Hash.Default))
                                    ?.GetPendingBlocks() ??
                                _branchedChains.First().GetPendingBlocks();
            }
        }

        private List<PendingBlock> AddBlockToBranchedChains(PendingBlock pendingBlock)
        {
            PendingBlocks.Print();

            _logger?.Trace(
                $"Adding to branched chain: {pendingBlock.Block.GetHash().ToHex()} : {pendingBlock.Block.Header.Index}");

            if (_branchedChains.Count == 0)
            {
                _branchedChains.Add(new BranchedChain(pendingBlock));
                return null;
            }

            var preBlockHash = pendingBlock.Block.Header.PreviousBlockHash;
            var blockHash = pendingBlock.Block.Header.GetHash();

            var toRemove = new List<BranchedChain>();
            var toAdd = new List<BranchedChain>();

            foreach (var branchedChain in _branchedChains)
            {
                if (branchedChain.GetPendingBlocks().First().Block.Header.PreviousBlockHash == blockHash)
                {
                    var newBranchedChain = new BranchedChain(pendingBlock, branchedChain.GetPendingBlocks());
                    toAdd.Add(newBranchedChain);
                    toRemove.Add(branchedChain);
                }
                else if (branchedChain.GetPendingBlocks().Last().Block.GetHash() == preBlockHash)
                {
                    var newBranchedChain = new BranchedChain(branchedChain.GetPendingBlocks(), pendingBlock);
                    toAdd.Add(newBranchedChain);
                    toRemove.Add(branchedChain);
                }
                else
                {
                    if (toAdd.Any(c => c.GetPendingBlocks().Any(pd => pd.Block.GetHash() == blockHash)))
                    {
                        continue;
                    }

                    toAdd.Add(new BranchedChain(pendingBlock));
                }
            }

            foreach (var branchedChain in toRemove)
            {
                _branchedChains.Remove(branchedChain);
            }

            foreach (var branchedChain in toAdd)
            {
                _branchedChains.Add(branchedChain);
            }

            var result = AdjustBranchedChains();
            if (result == null)
                return null;

            if (SyncedHeight < result.StartHeight)
            {
                var oldBlocks = new List<PendingBlock>();
                // Replace the pending blocks with the result
                foreach (var branchedBlock in result.GetPendingBlocks())
                {
                    if (PendingBlockHeight >= branchedBlock.Block.Header.Index)
                    {
                        var corresponding =
                            PendingBlocks.First(pb => pb.Block.Header.Index == branchedBlock.Block.Header.Index);
                        PendingBlocks.Remove(corresponding);
                        oldBlocks.Add(corresponding);
                    }

                    PendingBlocks.Add(branchedBlock);
                }

                _branchedChains.Add(new BranchedChain(oldBlocks));
            }

            PendingBlockHeight = result.EndHeight;
            _branchedChains.Remove(result);
            return result.GetPendingBlocks();
        }

        private BranchedChain AdjustBranchedChains()
        {
            var preBlockHashes = new List<Hash>();
            var lastBlockHashes = new List<Hash>();
            foreach (var branchedChain in _branchedChains)
            {
                preBlockHashes.Add(branchedChain.PreBlockHash);
                lastBlockHashes.Add(branchedChain.LastBlockHash);
            }

            var same = new List<Hash>();
            foreach (var preBlockHash in preBlockHashes)
            {
                foreach (var lastBlockHash in lastBlockHashes)
                {
                    if (preBlockHash == lastBlockHash)
                    {
                        same.Add(preBlockHash);
                    }
                }
            }

            foreach (var hash in same)
            {
                var chain1 = _branchedChains.First(c => c.PreBlockHash == hash);
                var chain2 = _branchedChains.First(c => c.LastBlockHash == hash);
                _branchedChains.Remove(chain1);
                _branchedChains.Remove(chain2);
                _branchedChains.Add(new BranchedChain(chain1.GetPendingBlocks(), chain2.GetPendingBlocks()));
            }

            if (PendingBlocks.IsEmpty())
            {
                return null;
            }

            foreach (var branchedChain in _branchedChains)
            {
                if (branchedChain.CanCheckout(PendingBlockHeight, PendingBlocks.Last().Block.GetHash()))
                {
                    _logger?.Trace("Switching chain.");
                    return branchedChain;
                }
            }

            return null;
        }

        public List<PendingBlock> GetPendingBlocksFromBranchedChains()
        {
            var pendingBlocks = new List<PendingBlock>();

            foreach (var chain in _branchedChains)
            {
                pendingBlocks.AddRange(chain.GetPendingBlocks());
            }

            return pendingBlocks;
        }
    }
}