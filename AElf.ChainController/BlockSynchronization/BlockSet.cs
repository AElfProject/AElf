using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration;
using AElf.Kernel;
using Easy.MessageHub;
using NLog;

// ReSharper disable once CheckNamespace
namespace AElf.ChainController
{
    public class BlockSet : IBlockSet
    {
        private readonly ILogger _logger;

        private readonly Queue<IBlock> _validBlock = new Queue<IBlock>();

        private readonly Dictionary<string, IBlock> _dict = new Dictionary<string, IBlock>();

        /// <summary>
        /// (Block height, Block hash) - 
        /// </summary>
        private readonly Dictionary<Tuple<ulong, string>, IBlock> _blockDict =
            new Dictionary<Tuple<ulong, string>, IBlock>();
        
        public BlockSet()
        {
            _logger = LogManager.GetLogger(nameof(BlockSet));
        }
        
        public async Task AddBlock(IBlock block)
        {
            _logger?.Trace($"Added block {block.GetHash().DumpHex()} to BlockSet.");
            _dict.Add(block.GetHash().DumpHex(), block);

            // TODO: Need a way to organize branched chains (using indexes)
        }

        /// <summary>
        /// Tell the block collection the height of block just successfully executed.
        /// </summary>
        /// <param name="currentHeight"></param>
        /// <returns></returns>
        public async Task Tell(ulong currentHeight)
        {
            RemoveOldBlocks(currentHeight - (ulong) GlobalConfig.BlockNumberOfEachRound);
        }

        public bool IsBlockReceived(Hash blockHash, ulong height)
        {
            return _blockDict.ContainsKey(Tuple.Create(height, blockHash.DumpHex()));
        }

        public IBlock GetBlockByHash(Hash blockHash)
        {
            return _dict.TryGetValue(blockHash.DumpHex(), out var block) ? block : null;
        }

        private void RemoveOldBlocks(ulong targetHeight)
        {
            
        }
    }
}