using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Kernel;
using Easy.MessageHub;
using NLog;

namespace AElf.Synchronization.BlockSynchronization
{
    public class LibChangedArgs : EventArgs
    {
        public BlockState NewLib { get; set; }
    }
    
    public class BlockSet
    {
        public event EventHandler LibChanged;
            
        private const int MaxLenght = 200;
        private const int Timeout = int.MaxValue;

        private readonly ILogger _logger;

        public BlockState CurrentHead { get; private set; }
        public BlockState CurrentLib { get; private set; }
        
        private List<BlockState> _blocks;

        private ReaderWriterLock _rwLock = new ReaderWriterLock();

        public ulong KeepHeight { get; set; } = ulong.MaxValue;
        
        private List<string> _miners;

        public BlockSet()
        {
            _logger = LogManager.GetLogger(nameof(BlockSet));
        }
        
        public BlockState Init(List<string> miners, IBlock currentDbBlock)
        {
            if (miners.Count <= 0)
                throw new ArgumentException("Miners is empty");
            
            _miners = miners.ToList();
            _blocks = new List<BlockState>();
            
            CurrentHead = new BlockState(currentDbBlock, null, true, _miners);
            _blocks.Add(CurrentHead);
            
            if (currentDbBlock.Index == GlobalConfig.GenesisBlockHeight)
                CurrentLib = CurrentHead;
            
            return CurrentHead;
        }

        public void PushBlock(IBlock block, bool isMined = false)
        {
            _rwLock.AcquireReaderLock(Timeout);
            
            try
            {
                var newBlockHash = block.GetHash();
                if (_blocks.Any(b => newBlockHash == b.BlockHash))
                    return;
                
                var previous = _blocks.FirstOrDefault(pb => pb.BlockHash == block.Header.PreviousBlockHash);
                
                if (previous == null)
                    throw new UnlinkableBlockException();
    
                // change the head
                BlockState newState;
                if (previous == CurrentHead)
                {
                    // made current chain longer
                    newState = new BlockState(block, previous, true, _miners);
                    CurrentHead = newState;
                }
                else
                {
                    // made another chain longer
                    newState = new BlockState(block, previous, false, _miners);
                    
                    // if this other chain becomes higher than the head -> switch
                    if (newState.Index > CurrentHead.Index)
                    {
                        _logger?.Debug($"Switching chain ({CurrentHead.BlockHash} -> {newState.BlockHash})");
                        CurrentHead = newState;
                    }
                }

                var lc = _rwLock.UpgradeToWriterLock(Timeout);
                
                try
                {
                    _blocks.Add(newState);

                    if (isMined)
                        return;
                    
                    // update LIB
                    ulong libIndex = CurrentLib == null ? 0UL : CurrentLib.Index;
                    
                    var blocksToConfirm = _blocks
                        .Where(b => libIndex < b.Index && b.Index < CurrentHead.Index)
                        .OrderByDescending(b => b.Index).ToList();
                    
                    BlockState newLib = null;
                    foreach (var blk in blocksToConfirm)
                    {
                        var hasAll = blk.AddConfirmation(newState.Producer);
                        if (hasAll)
                        {
                            newLib = blk;
                            break;
                        }
                    }

                    if (newLib != null)
                    {
                        CurrentLib = newLib;
                        _blocks.RemoveAll(b => b.Index < newLib.Index);
                        
                        // todo clear branches
                        
                        FireLibChanged(newLib);
                    }
                }
                finally
                {
                    _rwLock.DowngradeFromWriterLock(ref lc);
                }
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }
        }
        
        private void FireLibChanged(BlockState blockState)
        {
            EventHandler handler = LibChanged;
            if (handler != null)
            {
                handler(this, new LibChangedArgs { NewLib = blockState });
            }
        }

        public List<BlockState> GetBranch(BlockState branchTip, BlockState other)
        {
            List<BlockState> branchList = new List<BlockState>();
            List<BlockState> otherList = new List<BlockState>();

            BlockState currentBranchList = branchTip;
            BlockState currentOtherList = other;

            while (currentBranchList.Index > currentOtherList.Index)
            {
                branchList.Add(currentBranchList.GetCopyBlockState());
                currentBranchList = currentBranchList.PreviousState;
            }

            while (currentOtherList.Index > currentBranchList.Index)
            {
                otherList.Add(currentOtherList.GetCopyBlockState());
                currentOtherList = currentOtherList.PreviousState;
            }

            while (currentBranchList.Previous != currentOtherList.Previous)
            {
                if (currentBranchList.Previous == null || currentOtherList.Previous == null)
                    throw new InvalidOperationException("Invalid branch list.");
                
                branchList.Add(currentBranchList.GetCopyBlockState());
                otherList.Add(currentOtherList.GetCopyBlockState());
                
                currentBranchList = currentBranchList.PreviousState;
                currentOtherList = currentOtherList.PreviousState;
            }
            
            branchList.Add(currentBranchList.PreviousState.GetCopyBlockState());

            return branchList;
        }

        public bool IsBlockReceived(IBlock block)
        {
            _rwLock.AcquireReaderLock(Timeout);
            
            bool res;
            try
            {
                var blockHash = block.GetHash();
                res = _blocks.Any(b => blockHash == b.BlockHash);
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }

            return res;
        }

        public IBlock GetBlockByHash(Hash blockHash)
        {
            _rwLock.AcquireReaderLock(Timeout);
            
            BlockState blockSate;
            try
            {
                blockSate = _blocks.FirstOrDefault(b => b.BlockHash == blockHash);
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }

            // todo review check blockbody (old behaviour)
            return blockSate?.BlockBody == null ? null : blockSate.GetClonedBlock();
        }

        // todo include IsCurrentBranch
        public IEnumerable<BlockState> GetBlocksByHeight(ulong height)
        {
            _rwLock.AcquireReaderLock(Timeout);
            
            try
            {
                return _blocks.Where(b => b.Index == height).ToList();
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }
        }

        public void RemoveInvalidBlock(IBlock block)
        {
            if (block == null)
                throw new ArgumentNullException();
            
            _rwLock.AcquireWriterLock(Timeout);
            
            try
            {
                var blockHash = block.GetHash();
                var toRemove = _blocks.RemoveAll(b => b.BlockHash == blockHash);
                
                // todo handle branch removal
//                var workingSet = _blocks.Where(b => b.Index >= block.Index).ToList();
//                
//                if (!workingSet.Any())
//                    return;
//                
//                List<BlockState> toRemove = new List<BlockState>();
//                
//                foreach (var blk in workingSet)
//                {
//                    _blocks.Remove(blk);
//                    _logger?.Trace($"Removed block {blk.BlockHash} from block cache.");
//                }

            }
            finally
            {
                _rwLock.ReleaseWriterLock();
            }
        }
        
//        public BlockState UpdateCurrentHead()
//        {
//            // todo bad algo, refactor when refactor the block sync
//            List<BlockState> orderedBlocks = _blocks.OrderByDescending(b => b.Index).ToList();
//
//            if (orderedBlocks.Count <= 0)
//                return null;
//            
//            ulong highestBlockIndex = orderedBlocks.First().Index;
//            
//            // find the first N blocks where N.Index == highestBlockIndex;
//            List<BlockState> highestBlocks = new List<BlockState>();
//            foreach (var block in orderedBlocks)
//            {
//                if (block.Index == highestBlockIndex)
//                    highestBlocks.Add(block);
//                else
//                    break;
//            }
//
//            if (highestBlocks.Count == 1)
//            {
//                if (highestBlocks.ElementAt(0) == _currentHead)
//                    return null; // one block is higher and it's the current head -> no switch needed.
//                
//                // this one block is not the head
//                _currentHead = highestBlocks.ElementAt(0);
//                return _currentHead;
//            }
//            else
//            {
//                // more than one we return the current head. Find current head there:
//                var curr = highestBlocks.Where(b => b == _currentHead);
//
//                if (curr != null)
//                {
//                    // current block is part of the list of highest blocks (same hight)
//                    // so no need to switch
//                    return null;
//                }
//                
//                // Here we have to switch to the head that has _currentHead as ancestor (normaly previous block)
//                // for all 
//                
//            }
//        }
    }
}