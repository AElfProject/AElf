using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController.EventMessages;
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

        private bool _isExecuting;
        private bool _doingRollback;

        private bool _validatingOwnBlock;
        private bool _executingAgain;

        private bool LetItGo => _validatingOwnBlock || _executingAgain;

        public BlockValidationService(IEnumerable<IBlockValidationFilter> filters)
        {
            _filters = filters;

            _logger = LogManager.GetLogger(nameof(BlockValidationService));

            MessageHub.Instance.Subscribe<RollBackStateChanged>(inState => { _doingRollback = inState.DoingRollback; });
            MessageHub.Instance.Subscribe<ExecutionStateChanged>(inState => { _isExecuting = inState.IsExecuting; });
        }

        public async Task<BlockValidationResult> ValidateBlockAsync(IBlock block, IChainContext context)
        {
            if (!LetItGo)
            {
                if (_isExecuting)
                {
                    _logger?.Trace("Could not validate block during executing.");
                    return context.BlockHash.DumpHex() == block.BlockHashToHex
                        ? BlockValidationResult.AlreadyExecuted
                        : BlockValidationResult.IsExecuting;
                }
            }

            if (_doingRollback)
            {
                _logger?.Trace("Could not validate block during rollbacking!");
                return BlockValidationResult.DoingRollback;
            }

            MessageHub.Instance.Publish(new ValidationStateChanged(block.BlockHashToHex, block.Index, true,
                BlockValidationResult.Success));

            var resultCollection = new List<BlockValidationResult>();
            foreach (var filter in _filters)
            {
                var result = await filter.ValidateBlockAsync(block, context);
                _logger?.Trace($"Result of {filter.GetType().Name}: {result} - {block.BlockHashToHex}");
                resultCollection.Add(result);
            }

            var finalResult = resultCollection.Max();

            MessageHub.Instance.Publish(new ValidationStateChanged(block.BlockHashToHex, block.Index, false,
                finalResult));

            _validatingOwnBlock = false;
            _executingAgain = false;

            return finalResult;
        }

        public IBlockValidationService ValidatingOwnBlock(bool flag)
        {
            _validatingOwnBlock = flag;
            return this;
        }

        public IBlockValidationService ExecutingAgain(bool flag)
        {
            _executingAgain = flag;
            return this;
        }
    }
}