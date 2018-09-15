using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using AElf.ChainController;
using AElf.Common.Extensions;
using AElf.Kernel;
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
        private readonly List<BranchedChain> _branchedChains = new List<BranchedChain>();

        /// <summary>
        /// To track the latest block height of local chain.
        /// </summary>
        public ulong PendingBlockHeight { get; set; }

        public ulong SyncedHeight { get; set; }

        public List<PendingBlock> PendingBlocks { get; set; } = new List<PendingBlock>();
        
        public int Count => PendingBlocks.Count;
        public int BranchedChainsCount => _branchedChains.Count;

        private readonly ILogger _logger;

        public BlockCollection(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Basically add the pending block if the block is supposed to be on local chain.
        /// Otherwise add the pending block to pending fork block or branched chain.
        /// </summary>
        /// <param name="pendingBlock"></param>
        public void AddPendingBlock(PendingBlock pendingBlock)
        {
            // If this node isn't DPoS initial node, will add target pending block at the very beginning.
            if (PendingBlocks.IsNullOrEmpty())
            {
                PendingBlocks.AddPendingBlock(pendingBlock);
                return;
            }
            
            // No need to handle an already exists pending block again.
            if (PendingBlocks.Any(b => new Hash(b.BlockHash) == new Hash(pendingBlock.BlockHash)))
            {
                return;
            }

            // Normally add the pending block to list.
            if (PendingBlocks.AddPendingBlock(pendingBlock))
            {
                PendingBlockHeight = Math.Max(PendingBlockHeight, pendingBlock.Block.Header.Index);
            }
            else
            {
                AddBlockToBranchedChains(pendingBlock);
            }
        }

        /// <summary>
        /// Add the pending block to branched chain after removing.
        /// </summary>
        /// <param name="pendingBlock"></param>
        public void RemovePendingBlock(PendingBlock pendingBlock)
        {
            _logger?.Trace("Entering removing pending block");
            if (pendingBlock.ValidationError == ValidationError.Success)
            {
                SyncedHeight = pendingBlock.Block.Header.Index;
            }

            if (SyncedHeight == PendingBlockHeight || PendingBlockExtensions.IsConsensusGenerator)
            {
                _isInitialSync = false;
            }
            
            PendingBlocks.Remove(pendingBlock);
            _logger?.Trace($"Removing pending block: {pendingBlock.BlockHash.ToHex()} - {pendingBlock.Block.Header.Index}");
            PendingBlocks.Print();

            if (_isInitialSync && pendingBlock.Block.Header.Index > PendingBlockHeight)
            {
                AddBlockToBranchedChains(pendingBlock);
            }
            
            if (!_isInitialSync && PendingBlocks.Count <= 0 && BranchedChainsCount > 0)
            {
                PendingBlocks = _branchedChains.First(c => c.CanCheckout(PendingBlockHeight, Hash.Default))?.GetPendingBlocks() ??
                                _branchedChains.First().GetPendingBlocks();
            }
            _logger?.Trace("Leaving removing pending block");
        }

        //TODO: to optimize
        private void AddBlockToBranchedChains(PendingBlock pendingBlock)
        {
            PendingBlocks.Print();
            
            Console.WriteLine($"Adding to branched chain: {pendingBlock.Block.GetHash().ToHex()} : {pendingBlock.Block.Header.Index}");
            
            if (_branchedChains.Count == 0)
            {
                _branchedChains.Add(new BranchedChain(pendingBlock));
                return;
            }
            
            var preBlockHash = pendingBlock.Block.Header.PreviousBlockHash;
            Hash blockHash = pendingBlock.BlockHash;
            
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
                else if (branchedChain.GetPendingBlocks().Last().BlockHash == preBlockHash)
                {
                    var newBranchedChain = new BranchedChain(branchedChain.GetPendingBlocks(), pendingBlock);
                    toAdd.Add(newBranchedChain);
                    toRemove.Add(branchedChain);
                }
                else
                {
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
                return;

            PendingBlockHeight = PendingBlocks.Last().Block.Header.Index;
            _branchedChains.Remove(result);
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
                if (branchedChain.CanCheckout(PendingBlockHeight, PendingBlocks.Last().BlockHash))
                {
                    Console.WriteLine("Switched chain.");
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