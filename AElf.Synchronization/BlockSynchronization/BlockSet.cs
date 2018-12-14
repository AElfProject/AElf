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
    public class BlockState
    {
        private readonly IBlock _block;
        
        private Dictionary<byte[], int> _minersToConfirmations;

        public BlockBody BlockBody => _block?.Body; // todo refactor out
        public BlockHeader BlockHeader => _block?.Header; // todo refactor out

        public Hash Previous => _block.Header.PreviousBlockHash;
            
        public Hash BlockHash => _block.GetHash();
        public ulong Index => _block.Header.Index;

        public BlockState(IBlock block, BlockState previous)
        {
            _block = block.Clone();
            Init(previous);
        }

        private void Init(BlockState previous)
        {
            
        }
        
        public static bool operator ==(BlockState bs1, BlockState bs2)
        {
            return bs1?.Equals(bs2) ?? ReferenceEquals(bs2, null);
        }

        public static bool operator !=(BlockState bs1, BlockState bs2)
        {
            return !(bs1 == bs2);
        }
        
        public override bool Equals(Object obj)
        {
            var other = obj as BlockState;

            if (other == null)
                return false;

            // Instances are considered equal if the ReferenceId matches.
            return BlockHash == other.BlockHash;
        }

        public IBlock GetClonedBlock()
        {
            return _block.Clone();
        }
    }
    
    public class BlockSet : IBlockSet
    {
        private const int Timeout = int.MaxValue;
        
        public int InvalidBlockCount
        {
            get
            {
                int count;
                _rwLock.AcquireReaderLock(Timeout);
                try
                {
                    count = _blockCache.Count;
                }
                finally
                {
                    _rwLock.ReleaseReaderLock();
                }

                return count;
            }
        }

        public int ExecutedBlockCount
        {
            get
            {
                int count;
                _rwLock.AcquireReaderLock(Timeout);
                try
                {
                    count = _executedBlocks.Count;
                }
                finally
                {
                    _rwLock.ReleaseReaderLock();
                }

                return count;
            }
        }

        private readonly ILogger _logger;

        private static List<BlockState> _blockCache = new List<BlockState>();

        private readonly Dictionary<ulong, BlockState> _executedBlocks = new Dictionary<ulong, BlockState>();

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
                if (_blockCache.Any(b => newState == b))
                    return;

                var lc = _rwLock.UpgradeToWriterLock(Timeout);
                
                try
                {
                    _blockCache.Add(newState);
                    _blockCache = _blockCache.OrderBy(b => b.Index).ToList();
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
                _blockCache.RemoveAll(bs => bs == blockState);

                _blockCache.Add(blockState);
                _blockCache = _blockCache.OrderBy(b => b.Index).ToList();
            }
            finally
            {
                _rwLock.ReleaseWriterLock();
            }
            
            _logger?.Trace($"Add or update block {block.BlockHashToHex} to block cache.");
        }

        private void RemoveExecutedBlockFromCache(IBlock block)
        {
            var toRemove = _blockCache.FirstOrDefault(b => b.BlockHash == block.GetHash());
            
            if (toRemove == null)
                throw new InvalidOperationException("Block not present in block cache.");
            
            _rwLock.AcquireWriterLock(Timeout);

            try
            {
                _blockCache.Remove(toRemove);
                _executedBlocks.TryAdd(toRemove.Index, toRemove);
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
                res = _blockCache.Any(b => block.GetHash() == b.BlockHash);
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
                blockSate = _blockCache.FirstOrDefault(b => b.BlockHash == blockHash);
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
                if (_blockCache.Any(b => b.Index == height)) // todo refactor any / FirstOrDefault
                {
                    _blockCache.Where(b => b.Index == height).ForEach(b => blocks.Add(b.GetClonedBlock()));
                }
                else if (_executedBlocks.TryGetValue(height, out var bs) && bs?.BlockHeader != null)
                {
                    blocks.Add(bs.GetClonedBlock());
                }
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
                    for (var i = _executedBlocks.OrderBy(p => p.Key).FirstOrDefault().Key; i < targetHeight; i++)
                    {
                        if (_executedBlocks.ContainsKey(i))
                        {
                            _executedBlocks.RemoveKey(i);
                            _logger?.Trace($"Removed block of height {i} from executed block dict.");
                        }
                    }

                    var toRemove = _blockCache.Where(b => b.Index <= targetHeight).ToList();
                    
                    if (!toRemove.Any())
                        return;
                    
                    foreach (var block in toRemove)
                    {
                        _blockCache.Remove(block);
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

                var higherBlocks = _blockCache.Where(b => b.Index > rollbackHeight).OrderByDescending(b => b.Index)
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
                            if (_blockCache.Any(b => b.Index == blockToCheck.Index - 1 && b.BlockHash == blockToCheck.Previous)) // todo refactor any / FirstOrDefault
                            {
                                blockToCheck = _blockCache.FirstOrDefault(b => b.Index == blockToCheck.Index - 1 && b.BlockHash == blockToCheck.Previous);
                                
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
                    MessageHub.Instance.Publish(new UnlinkableHeader(_blockCache.First(b => b.Index == forkHeight).BlockHeader));
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

        /// <summary>
        /// Remove blocks attending rollback from executed blocks,
        /// and add them back to block cache.
        /// </summary>
        /// <param name="targetHeight"></param>
        /// <param name="currentHeight"></param>
        public void InformRollback(ulong targetHeight, ulong currentHeight)
        {
            // todo review acquire lock ?
            var toRemove = new List<BlockState>();
            for (var i = targetHeight; i < currentHeight; i++)
            {
                if (_executedBlocks.TryGetValue(i, out var block))
                {
                    toRemove.Add(block);
                }
            }

            foreach (var block in toRemove)
            {
                _rwLock.AcquireWriterLock(Timeout);
                
                try
                {
                    _executedBlocks.Remove(block.Index);
                }
                finally
                {
                    _rwLock.ReleaseWriterLock();
                }

                _logger?.Trace($"Removed block of height {block.Index} from executed block dict.");
                
                _blockCache.Add(block); // todo review no order by ?
                _logger?.Trace($"Added block {block.BlockHash} to block cache.");
            }
        }

        private void PrintInvalidBlockList()
        {
            var str = "\nInvalid Block List:\n";
            foreach (var block in _blockCache.OrderBy(b => b.Index))
            {
                str += $"{block.BlockHash} - {block.Index}\n\tPreBlockHash:{block.Previous}\n";
            }

            _logger?.Trace(str);
        }
    }
}