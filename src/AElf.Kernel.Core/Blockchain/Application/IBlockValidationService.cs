using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Blockchain.Application
{
    public interface IBlockValidationService
    {
        Task<bool> ValidateBlockBeforeAttachAsync(IBlock block);
        Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block);

        Task<bool> ValidateBlockAfterExecuteAsync(IBlock block);
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

        public async Task<bool> ValidateBlockBeforeAttachAsync(IBlock block)
        {
            foreach (var provider in _blockValidationProviders)
            {
                if (!await provider.ValidateBeforeAttachAsync(block))
                {
                    Logger.LogDebug($"Validate block before attach failed: {provider.GetType().Name}");
                    return false;
                }
            }

            return true;
        }

        public async Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block)
        {
            foreach (var provider in _blockValidationProviders)
            {
                if (!await provider.ValidateBlockBeforeExecuteAsync(block))
                {
                    Logger.LogDebug($"Validate block before execution failed: {provider.GetType().Name}");
                    return false;
                }
            }

            return true;
        }

        public async Task<bool> ValidateBlockAfterExecuteAsync(IBlock block)
        {
            foreach (var provider in _blockValidationProviders)
            {
                if (!await provider.ValidateBlockAfterExecuteAsync(block))
                {
                    Logger.LogDebug($"Validate block after execution failed: {provider.GetType().Name}");
                    return false;
                }
            }

            return true;
        }
    }
}