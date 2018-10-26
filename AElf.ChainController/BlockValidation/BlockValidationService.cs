using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController.EventMessages;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.EventMessages;
using Easy.MessageHub;
using NLog;

// ReSharper disable once CheckNamespace
namespace AElf.ChainController
{
    public class BlockValidationService : IBlockValidationService
    {
        private readonly IEnumerable<IBlockValidationFilter> _filters;
        private readonly ILogger _logger;

        private bool _isMining;
        private bool _doingRollback;

        private bool _validatingOwnBlock;

        public BlockValidationService(IEnumerable<IBlockValidationFilter> filters)
        {
            _filters = filters;

            _logger = LogManager.GetLogger(nameof(BlockValidationService));

            MessageHub.Instance.Subscribe<MiningStateChanged>(state => { _isMining = state.IsMining; });
            MessageHub.Instance.Subscribe<RollBackStateChanged>(state => { _doingRollback = state.DoingRollback; });
        }

        public async Task<BlockValidationResult> ValidateBlockAsync(IBlock block, IChainContext context)
        {
            if (_isMining && !_validatingOwnBlock)
            {
                _logger?.Trace("Mining!");
                if (context.BlockHash.DumpHex() == block.BlockHashToHex)
                {
                    return BlockValidationResult.AlreadyExecuted;
                }
                return BlockValidationResult.IsMining;
            }

            if (_doingRollback)
            {
                _logger?.Trace("Is rollbacking!");
                return BlockValidationResult.DoingRollback;
            }
            
            var resultCollection = new List<BlockValidationResult>();
            foreach (var filter in _filters)
            {
                var result = await filter.ValidateBlockAsync(block, context);
                _logger?.Trace($"Result of {filter.GetType().Name}: {result} - {block.BlockHashToHex}");
                resultCollection.Add(result);
            }

            return resultCollection.Max();
        }

        public IBlockValidationService ValidatingOwnBlock(bool flag)
        {
            _validatingOwnBlock = flag;

            return this;
        }
    }
}