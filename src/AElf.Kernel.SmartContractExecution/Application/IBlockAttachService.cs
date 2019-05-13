using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface IBlockAttachService
    {
        Task AttachBlockAsync(Block block);
        Task AttachReceivedBlockAsync(BlockWithTransactions blockWithTransactions);
    }

    public class BlockAttachService : IBlockAttachService, ITransientDependency
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockchainExecutingService _blockchainExecutingService;
        private readonly IBlockValidationService _blockValidationService;
        public ILogger<BlockAttachService> Logger { get; set; }

        public BlockAttachService(IBlockchainService blockchainService,
            IBlockchainExecutingService blockchainExecutingService, IBlockValidationService blockValidationService)
        {
            _blockchainService = blockchainService;
            _blockchainExecutingService = blockchainExecutingService;
            _blockValidationService = blockValidationService;
            Logger = NullLogger<BlockAttachService>.Instance;
        }

        public async Task AttachReceivedBlockAsync(BlockWithTransactions blockWithTransactions)
        {
            Block block = blockWithTransactions.ToBlock();
            
            if (!await ValidateAsync(block))
                return;
            
            await _blockchainService.AddBlockWithTransactionsAsync(blockWithTransactions);
            await AttachAndExecute(block);
        }

        public async Task AttachBlockAsync(Block block)
        {
            if (!await ValidateAsync(block))
                return;

            await _blockchainService.AddBlockAsync(block);
            await AttachAndExecute(block);
        }

        private async Task AttachAndExecute(Block block)
        {
            var chain = await _blockchainService.GetChainAsync();
            var status = await _blockchainService.AttachBlockToChainAsync(chain, block);
            await _blockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);
        }

        private async Task<bool> ValidateAsync(Block block)
        {
            var existBlock = await _blockchainService.GetBlockHeaderByHashAsync(block.GetHash());
            if (existBlock != null)
            {
                Logger.LogDebug($"Try attaching block but already exist, {block}");
                return false;
            }
            if (! await _blockValidationService.ValidateBlockBeforeAttachAsync(block))
            {
                Logger.LogWarning($"Validate block failed (before attach to chain), {block}");
                return false;
            }

            return true;
        }
    }
}