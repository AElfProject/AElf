using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Application;
using AElf.CSharp.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.Consensus.DPoS.Application
{
    // ReSharper disable InconsistentNaming
    public class BestChainFoundEventHandler : ILocalEventHandler<BestChainFoundEventData>, ITransientDependency
    {
        private readonly ITaskQueueManager _taskQueueManager;
        private readonly IIrreversibleBlockDiscoveryService _irreversibleBlockDiscoveryService;

        private readonly IBlockchainService _blockchainService;

        public BestChainFoundEventHandler(IIrreversibleBlockDiscoveryService irreversibleBlockDiscoveryService,
            ITaskQueueManager taskQueueManager, IBlockchainService blockchainService)
        {
            _irreversibleBlockDiscoveryService = irreversibleBlockDiscoveryService;
            _taskQueueManager = taskQueueManager;
            _blockchainService = blockchainService;
        }

        public async Task HandleEventAsync(BestChainFoundEventData eventData)
        {
            var chain = await _blockchainService.GetChainAsync();
            var index = await _irreversibleBlockDiscoveryService.DiscoverAndSetIrreversibleAsync(chain,
                eventData.ExecutedBlocks);
            
            if (index != null)
            {
                _taskQueueManager.Enqueue(
                    async () =>
                    {
                        var currentChain = await _blockchainService.GetChainAsync();
                        if (currentChain.LastIrreversibleBlockHeight < index.Height)
                        {
                            await _blockchainService.SetIrreversibleBlockAsync(currentChain, index.Height, index.Hash);
                        }
                    }, KernelConsts.UpdateChainQueueName);
            }
        }
    }
}