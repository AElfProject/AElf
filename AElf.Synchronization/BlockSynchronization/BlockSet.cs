using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Kernel;
using Akka.Util.Internal;
using Easy.MessageHub;
using NLog;

namespace AElf.Synchronization.BlockSynchronization
{
    public class BlockSet : IBlockSet
    {
        private const int MaxLenght = 200;
        private const int Timeout = int.MaxValue;

        private readonly ILogger _logger;

        private static List<BlockState> _blocks = new List<BlockState>();

        private ReaderWriterLock _rwLock = new ReaderWriterLock();
        private static int _flag;

        public ulong KeepHeight { get; set; } = ulong.MaxValue;

        public BlockSet()
        {
            _logger = LogManager.GetLogger(nameof(BlockSet));
        }

        public void AddBlock(IBlock block)
        {
            BlockState newState = new BlockState(block, null); // todo null
            
            _rwLock.AcquireReaderLock(Timeout);
            
            try
            {
                if (_blocks.Any(b => newState == b))
                    return;

                var lc = _rwLock.UpgradeToWriterLock(Timeout);
                
                try
                {
                    _blocks.Add(newState);
                    _blocks = _blocks.OrderBy(b => b.Index).ToList();
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

        public void AddOrUpdateMinedBlock(IBlock block)
        {
            if (block?.Header == null)
                throw new ArgumentNullException(nameof(block), "Null block or block header.");
            
            _rwLock.AcquireWriterLock(Timeout);
            
            try
            {
                BlockState blockState = new BlockState(block, null); // todo null confirmations
                
                // todo review old behaviour would first remove any with the same hash (??)
                _blocks.RemoveAll(bs => bs == blockState);

                _blocks.Add(blockState);
                _blocks = _blocks.OrderBy(b => b.Index).ToList();
            }
            finally
            {
                _rwLock.ReleaseWriterLock();
            }
            
            _logger?.Trace($"Add or update block {block.BlockHashToHex} to block cache.");
        }

        private void RemoveExecutedBlockFromCache(IBlock block)
        {
            var toRemove = _blocks.FirstOrDefault(b => b.BlockHash == block.GetHash());
            
            if (toRemove == null)
                throw new InvalidOperationException("Block not present in block cache.");
            
            _rwLock.AcquireWriterLock(Timeout);

            try
            {
                _blocks.Remove(toRemove);
            }
            finally
            {
                _rwLock.ReleaseWriterLock();
            }
            
            _logger?.Trace($"Transfered {toRemove.BlockHash.DumpHex()} from block cache to executed.");
        }

        /// <summary>
        /// Tell the block collection the height of block just successfully executed or mined.
        /// </summary>
        /// <param name="currentExecutedBlock"></param>
        /// <returns></returns>
        public void Tell(IBlock currentExecutedBlock)
        {
            RemoveExecutedBlockFromCache(currentExecutedBlock);

            if (currentExecutedBlock.Index >= KeepHeight)
                RemoveOldBlocks(currentExecutedBlock.Index - KeepHeight);
        }

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

        // todo refactor this 
        public IEnumerable<IBlock> GetBlocksByHeight(ulong height)
        {
            _rwLock.AcquireReaderLock(Timeout);
            
            var blocks = new List<IBlock>();
            try
            {
                _blocks.Where(b => b.Index == height).ForEach(b => blocks.Add(b.GetClonedBlock()));
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }

            return blocks;
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

        /// <summary>
        /// Return the fork height if exists longer chain.
        /// </summary>
        /// <param name="rollbackHeight"></param>
        /// <returns></returns>
        public ulong AnyLongerValidChain(ulong rollbackHeight)
        {
            var lockWasTaken = false;
            try
            {
                lockWasTaken = Interlocked.CompareExchange(ref _flag, 1, 0) == 0;
                if (!lockWasTaken)
                    return 0;

                var currentHeight = rollbackHeight + GlobalConfig.ForkDetectionLength;

                PrintInvalidBlockList();

                ulong forkHeight = 0;

                var higherBlocks = _blocks.Where(b => b.Index > rollbackHeight).OrderByDescending(b => b.Index)
                    .ToList();

                if (higherBlocks.Any())
                {
                    _logger?.Trace("Find higher blocks in block set, will check whether there are longer valid chain.");

                    // Get the index of highest block in block set.
                    var blockToCheck = higherBlocks.First();

                    while (true)
                    {
                        _rwLock.AcquireReaderLock(Timeout);
                        
                        try
                        {
                            // If a linkable block can be found in the invalid block list, update blockToCheck and indexToCheck.
                            if (_blocks.Any(b => b.Index == blockToCheck.Index - 1 && b.BlockHash == blockToCheck.Previous)) // todo refactor any / FirstOrDefault
                            {
                                blockToCheck = _blocks.FirstOrDefault(b => b.Index == blockToCheck.Index - 1 && b.BlockHash == blockToCheck.Previous);
                                
                                if (blockToCheck?.BlockHeader == null) // todo ??
                                    break;
                                
                                forkHeight = blockToCheck.Index;
                            }
                            else
                            {
                                break;
                            }
                        }
                        finally
                        {
                            _rwLock.ReleaseReaderLock();
                        }
                    }
                }

                _logger?.Trace($"Fork height: {forkHeight}");
                _logger?.Trace($"Current height: {currentHeight}");

                if (forkHeight > currentHeight)
                {
                    _logger?.Trace("No proper fork height.");
                    return 0;
                }

                if (forkHeight + GlobalConfig.BlockCacheLimit < currentHeight)
                {
                    KeepHeight = ulong.MaxValue;
                    MessageHub.Instance.Publish(new UnlinkableHeader(_blocks.First(b => b.Index == forkHeight).BlockHeader));
                }

                return forkHeight;
            }
            finally
            {
                if (lockWasTaken)
                {
                    Thread.VolatileWrite(ref _flag, 0);
                }
            }
        }

        private void PrintInvalidBlockList()
        {
            var str = "\nInvalid Block List:\n";
            foreach (var block in _blocks.OrderBy(b => b.Index))
            {
                str += $"{block.BlockHash} - {block.Index}\n\tPreBlockHash:{block.Previous}\n";
            }

            _logger?.Trace(str);
        }

        public bool IsFull()
        {
            return _blocks.Count > MaxLenght;
        }
    }
}