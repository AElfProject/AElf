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

        private bool _doingRollback;

        public BlockValidationService(IEnumerable<IBlockValidationFilter> filters)
        {
            _filters = filters;

            _logger = LogManager.GetLogger(nameof(BlockValidationService));

            MessageHub.Instance.Subscribe<RollBackStateChanged>(inState => { _doingRollback = inState.DoingRollback; });
        }

        public async Task<BlockValidationResult> ValidateBlockAsync(IBlock block, IChainContext context)
        {
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
                if(result != BlockValidationResult.Success)
                    _logger?.Warn($"Result of {filter.GetType().Name}: {result} - {block.BlockHashToHex}");
                resultCollection.Add(result);
            }

            var finalResult = resultCollection.Max();

            MessageHub.Instance.Publish(new ValidationStateChanged(block.BlockHashToHex, block.Index, false,
                finalResult));

            return finalResult;
        }
    }
}