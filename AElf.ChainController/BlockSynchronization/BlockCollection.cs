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
        private readonly IChainService _chainService;
        
        private readonly PendingBlocks _pendingBlocks = new PendingBlocks();
        private readonly List<BranchedChain> _branchedChains = new List<BranchedChain>();
        
        private IBlockChain _blockChain;

        private IBlockChain BlockChain => _blockChain ?? (_blockChain =
                                              _chainService.GetBlockChain(
                                                  Hash.LoadHex(NodeConfig.Instance.ChainId)));

        public BlockCollection(IChainService chainService)
        {
            _chainService = chainService;
            _logger = LogManager.GetLogger(nameof(BlockCollection));
        }
        
        public async Task AddBlock(IBlock block)
        {
            await _pendingBlocks.AddBlock(block);
        }
    }
}