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
    public class BlockCollection : IBlockCollection
    {
        private readonly ILogger _logger;

        private readonly HashSet<IBlock> _blocks = new HashSet<IBlock>();
        
        public BlockCollection()
        {
            _logger = LogManager.GetLogger(nameof(BlockCollection));
        }
        
        public async Task AddBlock(IBlock block)
        {
            _blocks.Add(block);
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

        private void RemoveOldBlocks(ulong targetHeight)
        {
            _blocks.RemoveWhere(b => b.Header.Index < targetHeight);
        }
    }
}