using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel
{
    public class LibBestChainFoundEventHandler : ILocalEventHandler<BestChainFoundEventData>, ITransientDependency
    {
        private readonly IBlockManager _blockManager;
        private readonly ITransactionResultManager _transactionResultManager;

        public ILocalEventBus LocalEventBus { get; set; }
        
        public LibBestChainFoundEventHandler(IBlockManager blockManager,
            ITransactionResultManager transactionResultManager)
        {
            _blockManager = blockManager;
            _transactionResultManager = transactionResultManager;
            LocalEventBus = NullLocalEventBus.Instance;
        }


        public async Task HandleEventAsync(BestChainFoundEventData eventData)
        {
            foreach (var executedBlock in eventData.ExecutedBlocks)
            {
                var block = await _blockManager.GetBlockAsync(executedBlock);

                foreach (var transactionHash in block.Body.Transactions)
                {
                    var result = await _transactionResultManager.GetTransactionResultAsync(transactionHash);
                    foreach (var contractEvent in result.Logs)
                    {
                        await LocalEventBus.PublishAsync(contractEvent);
                    }
                }
            }
        }
    }
}