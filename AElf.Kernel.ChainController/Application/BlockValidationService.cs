using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.ChainController.Application.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.ChainController.Application
{
    public class BlockValidationService : IBlockValidationService
    {
        private readonly IEnumerable<IBlockValidationFilter> _filters;
        public ILogger<BlockValidationService> Logger { get; set; }


        public BlockValidationService(IEnumerable<IBlockValidationFilter> filters)
        {
            _filters = filters;

            Logger = NullLogger<BlockValidationService>.Instance;

        }

        public async Task<BlockValidationResult> ValidateBlockAsync(IBlock block)
        {

            var resultCollection = new List<BlockValidationResult>();
            foreach (var filter in _filters)
            {
                var result = await filter.ValidateBlockAsync(block);
                if (result != BlockValidationResult.Success)
                    Logger.LogWarning($"Result of {filter.GetType().Name}: {result} - {block.BlockHashToHex}");
                resultCollection.Add(result);
            }

            var finalResult = resultCollection.Max();

            return finalResult;
        }
    }
}