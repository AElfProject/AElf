using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using NLog;

// ReSharper disable once CheckNamespace
namespace AElf.ChainController
{
    public class BlockValidationService : IBlockValidationService
    {
        private readonly IEnumerable<IBlockValidationFilter> _filters;
        private readonly ILogger _logger;

        public BlockValidationService(IEnumerable<IBlockValidationFilter> filters)
        {
            _filters = filters;

            _logger = LogManager.GetLogger(nameof(BlockValidationService));
        }

        public async Task<BlockValidationResult> ValidateBlockAsync(IBlock block, IChainContext context)
        {
            var resultCollection = new List<BlockValidationResult>();
            foreach (var filter in _filters)
            {
                var result = await filter.ValidateBlockAsync(block, context);
                _logger?.Trace($"Result of {filter.GetType().Name}: {result}");
                resultCollection.Add(result);
            }
            
            return resultCollection.Max();
        }
    }
}