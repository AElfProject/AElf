using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Common;
using AElf.Kernel;
using Akka.Util.Internal;
using NLog;

namespace AElf.Synchronization.BlockSynchronization
{
    public class BlockSet
    {
        private const int MaxLenght = 200;
        private const int Timeout = int.MaxValue;

        private readonly ILogger _logger;

        public BlockState CurrentHead { get; private set; }
        private static List<BlockState> _blocks = new List<BlockState>();

        private ReaderWriterLock _rwLock = new ReaderWriterLock();
        private static int _flag;

        public ulong KeepHeight { get; set; } = ulong.MaxValue;

        public BlockSet()
        {
            _logger = LogManager.GetLogger(nameof(BlockSet));
        }
        
        public BlockState Init(Block currentDbBlock)
        {
            CurrentHead = new BlockState(currentDbBlock, null, true);
            _blocks.Add(CurrentHead);
            return CurrentHead;
        }

        public void PushBlock(IBlock block)
        {
            _rwLock.AcquireReaderLock(Timeout);
            
            try
            {
                var previous = _blocks.FirstOrDefault(pb => pb.BlockHash == block.Header.PreviousBlockHash);
                
                if (previous == null)
                    throw new UnlinkableBlockException();
    
                BlockState newState;
                if (previous == CurrentHead)
                {
                    // made current chain longer
                    newState = new BlockState(block, previous, true);
                    CurrentHead = newState;
                }
                else
                {
                    // made another chain longer
                    newState = new BlockState(block, previous, false);
                    
                    // if this other chain becomes higher than the head -> switch
                    if (newState.Index > CurrentHead.Index)
                        CurrentHead = newState;
                }
            
                if (_blocks.Any(b => newState == b))
                    return;

                var lc = _rwLock.UpgradeToWriterLock(Timeout);
                
                try
                {
                    _blocks.Add(newState);
                     //todo update LIB
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

        public void SwitchToCurrentHead()
        {
            if (CurrentHead.IsInCurrentBranch)
            {
                _logger?.Warn("Unexpected situation: current head already canonical.");
                return;
            }
            
            
        }
        
//        public void AddOrUpdateMinedBlock(IBlock block)
//        {
//            if (block?.Header == null)
//                throw new ArgumentNullException(nameof(block), "Null block or block header.");
//            
//            _rwLock.AcquireWriterLock(Timeout);
//            
//            try
//            {
//                BlockState blockState = new BlockState(block, null); // todo null confirmations
//                
//                // todo review old behaviour would first remove any with the same hash (??)
//                _blocks.RemoveAll(bs => bs == blockState);
//
//                _blocks.Add(blockState);
//                _blocks = _blocks.OrderBy(b => b.Index).ToList();
//            }
//            finally
//            {
//                _rwLock.ReleaseWriterLock();
//            }
//            
//            _logger?.Trace($"Add or update block {block.BlockHashToHex} to block cache.");
//        }

//        private void RemoveExecutedBlockFromCache(IBlock block)
//        {
//            var toRemove = _blocks.FirstOrDefault(b => b.BlockHash == block.GetHash());
//            
//            if (toRemove == null)
//                throw new InvalidOperationException("Block not present in block cache.");
//            
//            _rwLock.AcquireWriterLock(Timeout);
//
//            try
//            {
//                _blocks.Remove(toRemove);
//            }
//            finally
//            {
//                _rwLock.ReleaseWriterLock();
//            }
//            
//            _logger?.Trace($"Transfered {toRemove.BlockHash.DumpHex()} from block cache to executed.");
//        }

        /// <summary>
        /// Tell the block collection the height of block just successfully executed or mined.
        /// </summary>
        /// <param name="currentExecutedBlock"></param>
        /// <returns></returns>
//        public void Tell(IBlock currentExecutedBlock)
//        {
//            RemoveExecutedBlockFromCache(currentExecutedBlock);
//
//            if (currentExecutedBlock.Index >= KeepHeight)
//                RemoveOldBlocks(currentExecutedBlock.Index - KeepHeight);
//        }

        public bool IsBlockReceived(IBlock block)
        {
            _rwLock.AcquireReaderLock(Timeout);
            
            bool res;
            try
            {
                res = _blocks.Any(b => block.GetHash() == b.BlockHash);
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

        private void RemoveOldBlocks(ulong targetHeight)
        {
            try
            {
                _rwLock.AcquireWriterLock(Timeout);
                
                try
                {
                    var toRemove = _blocks.Where(b => b.Index <= targetHeight).ToList();
                    
                    if (!toRemove.Any())
                        return;
                    
                    foreach (var block in toRemove)
                    {
                        _blocks.Remove(block);
                        _logger?.Trace($"Removed block {block.BlockHash} from block cache.");
                    }
                    
                }
                finally
                {
                    _rwLock.ReleaseWriterLock();
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
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

        public bool IsFull()
        {
            return _blocks.Count > MaxLenght;
        }
    }
}