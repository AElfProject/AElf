using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Common.Extensions;
using AElf.Common.MultiIndexDictionary;
using AElf.Configuration;
using AElf.Kernel;
using Akka.Util.Internal;
using Easy.MessageHub;
using NLog;

// ReSharper disable once CheckNamespace
namespace AElf.ChainController
{
    public class BlockSet : IBlockSet
    {
        private readonly ILogger _logger;

        private readonly IndexedDictionary<IBlock> _dict;

        private readonly object _ = new object();

        public BlockSet()
        {
            _logger = LogManager.GetLogger(nameof(BlockSet));

            _dict = new IndexedDictionary<IBlock>();
            _dict.IndexBy(b => b.Index, true)
                .IndexBy(b => b.BlockHashToHex);
        }

        public void AddBlock(IBlock block)
        {
            var hash = block.GetHash().DumpHex();
            _logger?.Trace($"Added block {hash} to BlockSet.");
            lock (_)
            {
                _dict.Add(block);
            }
        }

        /// <summary>
        /// Tell the block collection the height of block just successfully executed.
        /// </summary>
        /// <param name="currentHeight"></param>
        /// <returns></returns>
        public void Tell(ulong currentHeight)
        {
            if (currentHeight <= (ulong) GlobalConfig.BlockNumberOfEachRound)
            {
                return;
            }
            
            RemoveOldBlocks(currentHeight - (ulong) GlobalConfig.BlockNumberOfEachRound);
        }

        public bool IsBlockReceived(Hash blockHash, ulong height)
        {
            lock (_)
            {
                return _dict.Any(b => b.Index == height && b.BlockHashToHex == blockHash.DumpHex());
            }
        }

        public IBlock GetBlockByHash(Hash blockHash)
        {
            lock (_)
            {
                return _dict.FirstOrDefault(b => b.BlockHashToHex == blockHash.DumpHex());
            }
        }

        public List<IBlock> GetBlockByHeight(ulong height)
        {
            lock (_)
            {
                return _dict.Where(b => b.Index == height).ToList();
            }
        }

        private void RemoveOldBlocks(ulong targetHeight)
        {
            IEnumerable<IBlock> removed;
            lock (_)
            {
                removed = _dict.RemoveWhere(b => b.Index <= targetHeight);
            }

            if (removed != null)
            {
                _logger?.Trace($"Removed {removed.Count()} blocks whose index lower than {targetHeight}");
            }
        }
    }
}