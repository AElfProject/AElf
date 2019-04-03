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
    }

    public class BlockAttachService : IBlockAttachService, ITransientDependency
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockchainExecutingService _blockchainExecutingService;
        public ILogger<BlockAttachService> Logger { get; set; }

        public BlockAttachService(IBlockchainService blockchainService,
            IBlockchainExecutingService blockchainExecutingService)
        {
            _blockchainService = blockchainService;
            _blockchainExecutingService = blockchainExecutingService;

            Logger = NullLogger<BlockAttachService>.Instance;
        }

        public async Task AttachBlockAsync(Block block)
        {
            var existBlock = await _blockchainService.GetBlockHeaderByHashAsync(block.GetHash());
            if (existBlock == null)
            {
                await _blockchainService.AddBlockAsync(block);
                var chain = await _blockchainService.GetChainAsync();
                var status = await _blockchainService.AttachBlockToChainAsync(chain, block);
                await _blockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);
            }
        }
    }
}