using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface IBlockAttachService
    {
        Task AttachBlockAsync(Block block);
    }

    public class BlockAttachService : IBlockAttachService, ITransientDependency
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockchainExecutingService _blockchainExecutingService;
        private readonly IChainBlockLinkService _chainBlockLinkService;
        private readonly IBlockExecutionResultProcessingService _blockExecutionResultProcessingService;
        
        public ILogger<BlockAttachService> Logger { get; set; }

        public BlockAttachService(IBlockchainService blockchainService,
            IBlockchainExecutingService blockchainExecutingService,
            IChainBlockLinkService chainBlockLinkService,
            IBlockExecutionResultProcessingService blockExecutionResultProcessingService)
        {
            _blockchainService = blockchainService;
            _blockchainExecutingService = blockchainExecutingService;
            _chainBlockLinkService = chainBlockLinkService;
            _blockExecutionResultProcessingService = blockExecutionResultProcessingService;

            Logger = NullLogger<BlockAttachService>.Instance;
        }

        public async Task AttachBlockAsync(Block block)
        {
            var chain = await _blockchainService.GetChainAsync();

            var status = await _blockchainService.AttachBlockToChainAsync(chain, block);
            if (!status.HasFlag(BlockAttachOperationStatus.LongestChainFound))
            {
                Logger.LogDebug($"Try to attach to chain but the status is {status}.");
                return;
            }

            var notExecutedChainBlockLinks =
                await _chainBlockLinkService.GetNotExecutedChainBlockLinksAsync(chain.LongestChainHash);
            var notExecutedBlocks =
                await _blockchainService.GetBlocksAsync(notExecutedChainBlockLinks.Select(l => l.BlockHash));

            var executionResult = new BlockExecutionResult();
            try
            {
                executionResult = await _blockchainExecutingService.ExecuteBlocksAsync(notExecutedBlocks);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Block execute fails.");
                throw;
            }
            finally
            {
                await _blockExecutionResultProcessingService.ProcessBlockExecutionResultAsync(chain, executionResult);
            }
        }
    }
}