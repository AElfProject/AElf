using System;
using System.Collections.Generic;
using System.Linq;
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
                    return _list.Count;
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

        private readonly List<IBlock> _list = new List<IBlock>();

        private readonly Dictionary<ulong, IBlock> _executedBlocks = new Dictionary<ulong, IBlock>();

        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private object _ = new object();

        private const ulong KeepHeight = 20;

        public BlockSet()
        {
            _logger = LogManager.GetLogger(nameof(BlockSet));
        }

        public void AddBlock(IBlock block)
        {
            var hash = block.BlockHashToHex;
            _logger?.Trace($"Added block {hash} to BlockSet.");
            lock (_)
            {
                _list.Add(block);
            }
        }

        public void RemoveExecutedBlock(string blockHashHex)
        {
            lock (_)
            {
                var toRemove = _list.FirstOrDefault(b => b.BlockHashToHex == blockHashHex);
                if (toRemove?.Header != null)
                {
                    _list.Remove(toRemove);
                    _executedBlocks.TryAdd(toRemove.Index, toRemove);
                }
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
                return _list.Any(b => b.Index == height && b.BlockHashToHex == blockHash.DumpHex());
            }
        }

        public IBlock GetBlockByHash(Hash blockHash)
        {
            lock (_)
            {
                return _list.FirstOrDefault(b => b.BlockHashToHex == blockHash.DumpHex());
            }
        }

        public List<IBlock> GetBlockByHeight(ulong height)
        {
            lock (_)
            {
                if (_list.Any(b => b.Index == height))
                {
                    return _list.Where(b => b.Index == height).ToList();
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
                    var toRemove = _list.Where(b => b.Index <= targetHeight).ToList();
                    if (!toRemove.Any())
                        return;
                    foreach (var block in toRemove)
                    {
                        _list.Remove(block);
                    }

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
            IEnumerable<IBlock> higherBlocks;
            ulong forkHeight = 0;

            lock (_)
            {
                higherBlocks = _list.Where(b => b.Index > currentHeight).OrderByDescending(b => b.Index).ToList();
            }

            if (higherBlocks.Any())
            {
                _logger?.Trace("Find higher blocks in block set, will check whether there are longer valid chain.");

                // Get the index of highest block in block set.
                var block = higherBlocks.First();
                var index = block.Index;

                var flag = true;
                while (flag)
                {
                    lock (_)
                    {
                        if (_list.Any(b => b.Index == index - 1))
                        {
                            var index1 = index;
                            var lowerBlock = _list.Where(b => b.Index == index1 - 1);
                            block = lowerBlock.FirstOrDefault(b => b.BlockHashToHex == block.Header.PreviousBlockHash.DumpHex());
                            if (block?.Header != null)
                            {
                                index--;
                                forkHeight = index;
                            }
                        }
                        else
                        {
                            flag = false;
                        }
                    }
                }
            }

            return forkHeight <= currentHeight ? forkHeight : 0;
        }
    }
}