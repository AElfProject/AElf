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
    // ReSharper disable FieldCanBeMadeReadOnly.Local
    public class BlockSet : IBlockSet
    {
        public int InvalidBlockCount
        {
            get
            {
                int count;
                _rwLock.AcquireReaderLock(100);
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
                _rwLock.AcquireReaderLock(100);
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

        private static List<IBlock> _blockCache = new List<IBlock>();

        //public static ulong? MaxHeight => _blockCache.OrderByDescending(b => b.Index).FirstOrDefault()?.Index;

        private readonly Dictionary<ulong, IBlock> _executedBlocks = new Dictionary<ulong, IBlock>();

        private ReaderWriterLock _rwLock = new ReaderWriterLock();

        private static int _flag;

        public ulong KeepHeight { get; set; } = ulong.MaxValue;

        public BlockSet()
        {
            _logger = LogManager.GetLogger(nameof(BlockSet));
        }

        public void AddBlock(IBlock block)
        {
            var hash = block.BlockHashToHex;
            _logger?.Trace($"Added block {hash} to block cache.");
            _rwLock.AcquireReaderLock(100);
            try
            {
                if (_blockCache.Any(b => b.BlockHashToHex == block.BlockHashToHex))
                    return;

                var lc = _rwLock.UpgradeToWriterLock(100);
                try
                {
                    _blockCache.Add(block);
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

        public void RemoveExecutedBlock(string blockHashHex)
        {
            var toRemove = _blockCache.FirstOrDefault(b => b.BlockHashToHex == blockHashHex);
            if (toRemove?.Header == null)
                return;
            _rwLock.AcquireWriterLock(100);
            try
            {
                _blockCache.Remove(toRemove);
                _executedBlocks.TryAdd(toRemove.Index, toRemove);
            }
            finally
            {
                _rwLock.ReleaseWriterLock();
                _logger?.Trace($"Removed block {blockHashHex} from block cache.");
                _logger?.Trace($"Added block {blockHashHex} to executed block dict.");
            }
        }

        /// <summary>
        /// Tell the block collection the height of block just successfully executed or mined.
        /// </summary>
        /// <param name="currentExecutedBlock"></param>
        /// <returns></returns>
        public void Tell(IBlock currentExecutedBlock)
        {
            RemoveExecutedBlock(currentExecutedBlock.BlockHashToHex);

            if (currentExecutedBlock.Index >= KeepHeight)
                RemoveOldBlocks(currentExecutedBlock.Index - KeepHeight);
        }

        public bool IsBlockReceived(Hash blockHash, ulong height)
        {
            bool res;
            _rwLock.AcquireReaderLock(100);
            try
            {
                res = _blockCache.Any(b => b.Index == height && b.BlockHashToHex == blockHash.DumpHex());
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }

            return res;
        }

        public IBlock GetBlockByHash(Hash blockHash)
        {
            IBlock block;
            _rwLock.AcquireReaderLock(100);
            try
            {
                block = _blockCache.FirstOrDefault(b => b.BlockHashToHex == blockHash.DumpHex());
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }

            return block?.Body == null ? null : block;
        }

        public List<IBlock> GetBlocksByHeight(ulong height)
        {
            _rwLock.AcquireReaderLock(100);
            try
            {
                if (_blockCache.Any(b => b.Index == height))
                {
                    return _blockCache.Where(b => b.Index == height).ToList();
                }

                if (_executedBlocks.TryGetValue(height, out var block) && block?.Header != null)
                {
                    return new List<IBlock> {block};
                }
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }

            return new List<IBlock>();
        }

        private void RemoveOldBlocks(ulong targetHeight)
        {
            try
            {
                _rwLock.AcquireWriterLock(100);
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
                        _logger?.Trace($"Removed block {block.BlockHashToHex} from block cache.");
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
        public ulong      AnyLongerValidChain(ulong rollbackHeight)
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
                    _logger?.Trace(
                        "Find higher blocks in block set, will check whether there are longer valid chain.");

                    // Get the index of highest block in block set.
                    var blockToCheck = higherBlocks.First();

                    while (true)
                    {
                        _rwLock.AcquireReaderLock(100);
                        try
                        {
                            // If a linkable block can be found in the invalid block list,
                            // update blockToCheck and indexToCheck.
                            if (_blockCache.Any(b => b.Index == blockToCheck.Index - 1 &&
                                                     b.BlockHashToHex == blockToCheck.Header.PreviousBlockHash
                                                         .DumpHex()))
                            {
                                blockToCheck = _blockCache.FirstOrDefault(b =>
                                    b.Index == blockToCheck.Index - 1 &&
                                    b.BlockHashToHex == blockToCheck.Header.PreviousBlockHash.DumpHex());
                                if (blockToCheck?.Header == null)
                                {
                                    break;
                                }

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
                }
                
                return forkHeight <= currentHeight ? forkHeight : 0;
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
            var toRemove = new List<IBlock>();
            for (var i = targetHeight; i < currentHeight; i++)
            {
                if (_executedBlocks.TryGetValue(i, out var block))
                {
                    toRemove.Add(block);
                }
            }

            foreach (var block in toRemove)
            {
                _rwLock.AcquireWriterLock(100);
                try
                {
                    _executedBlocks.Remove(block.Index);
                }
                finally
                {
                    _rwLock.ReleaseWriterLock();
                }

                _logger?.Trace($"Removed block of height {block.Index} from executed block dict.");
                _blockCache.Add(block);
                _logger?.Trace($"Added block {block.BlockHashToHex} to block cache.");
            }
        }

        private void PrintInvalidBlockList()
        {
            var str = "\nInvalid Block List:\n";
            foreach (var block in _blockCache.OrderBy(b => b.Index))
            {
                str +=
                    $"{block.BlockHashToHex} - {block.Index}\n\tPreBlockHash:{block.Header.PreviousBlockHash.DumpHex()}\n";
            }

            _logger?.Trace(str);
        }
    }
}