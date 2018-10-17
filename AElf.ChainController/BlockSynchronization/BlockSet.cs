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
            throw new NotImplementedException();
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

        private void RemoveOldBlocks(ulong targetHeight)
        {
            throw new NotImplementedException();
        }
    }
}