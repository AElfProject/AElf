using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Blockchain.Application
{
    public interface IBlockValidationService
    {
        Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block);

        Task<bool> ValidateBlockAfterExecuteAsync(IBlock block);
    }

    public class BlockValidationService : IBlockValidationService, ITransientDependency
    {
        private readonly IEnumerable<IBlockValidationProvider> _blockValidationProviders;

        public BlockValidationService(IEnumerable<IBlockValidationProvider> blockValidationProviders)
        {
            Logger = NullLogger<BlockValidationService>.Instance;
            _blockValidationProviders = blockValidationProviders;
        }

        public ILogger<BlockValidationService> Logger { get; set; }

        public async Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block)
        {
            foreach (var provider in _blockValidationProviders)
                if (!await provider.ValidateBlockBeforeExecuteAsync(block))
                    return false;

            return true;
        }

        public async Task<bool> ValidateBlockAfterExecuteAsync(IBlock block)
        {
            foreach (var provider in _blockValidationProviders)
                if (!await provider.ValidateBlockAfterExecuteAsync(block))
                    return false;

            return true;
        }
    }
}