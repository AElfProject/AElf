using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface IBlockAttachService
    {
        void AttachBlock(Block block);
    }

    public class BlockAttachService : IBlockAttachService, ITransientDependency
    {      
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockchainExecutingService _blockchainExecutingService;
        private readonly ITaskQueueManager _taskQueueManager;
        
        public ILogger<BlockAttachService> Logger { get; set; }

        private const string BlockAttachQueueName = "BlockAttachQueue";

        public BlockAttachService(IBlockchainService blockchainService, 
            IBlockchainExecutingService blockchainExecutingService, 
            ITaskQueueManager taskQueueManager)
        {
            _blockchainService = blockchainService;
            _blockchainExecutingService = blockchainExecutingService;
            _taskQueueManager = taskQueueManager;
            
            Logger = NullLogger<BlockAttachService>.Instance;
        }

        public void AttachBlock(Block block)
        {
            Logger.LogDebug($"Put block in the queue. block: {block}");
            _taskQueueManager.GetQueue(BlockAttachQueueName).Enqueue(async () =>
            {
                var existBlock = await _blockchainService.GetBlockHeaderByHashAsync(block.GetHash());
                if (existBlock == null)
                {
                    await _blockchainService.AddBlockAsync(block);
                    var chain = await _blockchainService.GetChainAsync();
                    var status = await _blockchainService.AttachBlockToChainAsync(chain, block);
                    await _blockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);
                }
            });
        }
    }
}