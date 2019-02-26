using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Blockchain.Application
{
    public interface IBlockValidationService
    {
        Task<bool> ValidateBlockBeforeExecuteAsync(int chainId, IBlock block);

        Task<bool> ValidateBlockAfterExecuteAsync(int chainId, IBlock block);
    }

    public class BlockValidationService : IBlockValidationService, ITransientDependency
    {
        public ILogger<BlockValidationService> Logger { get; set; }

        private readonly IEnumerable<IBlockValidationProvider> _blockValidationProviders;

        public BlockValidationService(IEnumerable<IBlockValidationProvider> blockValidationProviders)
        {
            Logger = NullLogger<BlockValidationService>.Instance;
            _blockValidationProviders = blockValidationProviders;
        }

        public async Task<bool> ValidateBlockBeforeExecuteAsync(int chainId, IBlock block)
        {
            foreach (var provider in _blockValidationProviders)
            {
                if (!await provider.ValidateBlockBeforeExecuteAsync(chainId, block))
                    return false;
            }

            return true;
        }

        public async Task<bool> ValidateBlockAfterExecuteAsync(int chainId, IBlock block)
        {
            foreach (var provider in _blockValidationProviders)
            {
                if (!await provider.ValidateBlockAfterExecuteAsync(chainId, block))
                    return false;
            }

            return true;
        }
    }
}