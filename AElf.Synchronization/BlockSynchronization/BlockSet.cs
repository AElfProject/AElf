using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Common;
using AElf.Kernel;
using Akka.Util.Internal;
using NLog;

// ReSharper disable once CheckNamespace
namespace AElf.Synchronization.BlockSynchronization
{
    public class BlockSet : IBlockSet
    {
        public int InvalidBlockCount
        {
            get
            {
                lock (_)
                {
                    return _invalidBlockList.Count;
                }
            }
        }

        public int ExecutedBlockCount
        {
            get
            {
                lock (_)
                {
                    return _executedBlocks.Count;
                }
            }
        }

        private readonly ILogger _logger;

        private readonly List<IBlock> _invalidBlockList = new List<IBlock>();

        private readonly Dictionary<ulong, IBlock> _executedBlocks = new Dictionary<ulong, IBlock>();

        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private object _ = new object();

        private int _flag;

        public ulong KeepHeight { get; set; } = ulong.MaxValue;

        public BlockSet()
        {
            _logger = LogManager.GetLogger(nameof(BlockSet));
        }

        public void AddBlock(IBlock block)
        {
            var hash = block.BlockHashToHex;
            _logger?.Trace($"Added block {hash} to invalid block list.");
            lock (_)
            {
                if (_invalidBlockList.All(b => b.BlockHashToHex != block.BlockHashToHex))
                {
                    _invalidBlockList.Add(block);
                }
            }
        }

        public void RemoveExecutedBlock(string blockHashHex)
        {
            lock (_)
            {
                var toRemove = _invalidBlockList.FirstOrDefault(b => b.BlockHashToHex == blockHashHex);
                if (toRemove?.Header == null) 
                    return;
                
                _invalidBlockList.Remove(toRemove);
                _logger?.Trace($"Removed block {blockHashHex} from invalid block list.");
                _executedBlocks.TryAdd(toRemove.Index, toRemove);
                _logger?.Trace($"Add block {blockHashHex} to executed block dict.");
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
            lock (_)
            {
                return _invalidBlockList.Any(b => b.Index == height && b.BlockHashToHex == blockHash.DumpHex());
            }
        }

        public IBlock GetBlockByHash(Hash blockHash)
        {
            lock (_)
            {
                return _invalidBlockList.FirstOrDefault(b => b.BlockHashToHex == blockHash.DumpHex());
            }
        }

        public List<IBlock> GetBlockByHeight(ulong height)
        {
            lock (_)
            {
                if (_invalidBlockList.Any(b => b.Index == height))
                {
                    return _invalidBlockList.Where(b => b.Index == height).ToList();
                }

                if (_executedBlocks.TryGetValue(height, out var block) && block?.Header != null)
                {
                    return new List<IBlock> {block};
                }

                return null;
            }
        }

        private void RemoveOldBlocks(ulong targetHeight)
        {
            try
            {
                lock (_)
                {
                    var toRemove = _invalidBlockList.Where(b => b.Index <= targetHeight).ToList();
                    if (!toRemove.Any())
                        return;
                    foreach (var block in toRemove)
                    {
                        _invalidBlockList.Remove(block);
                        _logger?.Trace($"Removed block {block.BlockHashToHex} from invalid block list.");
                    }

                    _logger?.Trace($"Removed block of height {targetHeight} from executed block dict.");
                    _executedBlocks.RemoveKey(targetHeight);
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
        /// <param name="currentHeight"></param>
        /// <returns></returns>
        public ulong AnyLongerValidChain(ulong currentHeight)
        {
            var res = Interlocked.CompareExchange(ref _flag, 1, 0);
            if (res == 1)
                return 0;

            PrintInvalidBlockList();
            
            IEnumerable<IBlock> higherBlocks;
            ulong forkHeight = 0;

            lock (_)
            {
                higherBlocks = _invalidBlockList.Where(b => b.Index > currentHeight).OrderByDescending(b => b.Index)
                    .ToList();
            }

            if (higherBlocks.Any())
            {
                _logger?.Trace("Find higher blocks in block set, will check whether there are longer valid chain.");

                // Get the index of highest block in block set.
                var blockToCheck = higherBlocks.First();

                while (true)
                {
                    lock (_)
                    {
                        // If a linkable block can be found in the invalid block list,
                        // update blockToCheck and indexToCheck.
                        if (_invalidBlockList.Any(b => b.Index == blockToCheck.Index - 1 &&
                                 b.BlockHashToHex == blockToCheck.Header.PreviousBlockHash.DumpHex()))
                        {
                            blockToCheck = _invalidBlockList.FirstOrDefault(b =>
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
                        
/*                        if (_invalidBlockList.Any(b => b.Index == indexToCheck - 1))
                        {
                            var index1 = indexToCheck;
                            var lowerBlocks = _invalidBlockList.Where(b => b.Index == index1 - 1).ToList();
                            foreach (var lowerBlock in lowerBlocks)
                            {
                                _logger?.Trace("lower block: " + lowerBlock.BlockHashToHex);
                            }
                            blockToCheck = lowerBlocks.FirstOrDefault(b =>
                                blockToCheck != null && b.BlockHashToHex == blockToCheck.Header.PreviousBlockHash.DumpHex());
                            if (blockToCheck?.Header != null)
                            {
                                indexToCheck--;
                                forkHeight = indexToCheck;
                            }
                        }*/
                    }
                }
            }

            _logger?.Trace($"Fork height: {forkHeight}");
            _logger?.Trace($"Current height: {currentHeight}");

            if (forkHeight > currentHeight)
            {
                _logger?.Trace("No proper fork height.");
            }

            Interlocked.CompareExchange(ref _flag, 0, 1);

            return forkHeight <= currentHeight ? forkHeight : 0;
        }

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
                _executedBlocks.Remove(block.Index);
                _logger?.Trace($"Removed block of height {block.Index} from executed block dict.");
            }
        }

        public bool MultipleBlocksInOneIndex(ulong index)
        {
            return _invalidBlockList.Count(b => b.Index == index) > 1;
        }

        private void PrintInvalidBlockList()
        {
            var str = "\nInvalid Block List:\n";
            foreach (var block in _invalidBlockList)
            {
                str += $"{block.BlockHashToHex} - {block.Index}\n\tPreBlockHash:{block.Header.PreviousBlockHash.DumpHex()}\n";
            }
            _logger?.Trace(str);
        }
    }
}