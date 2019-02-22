using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Node.Domain;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS.Network.Events;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    namespace AElf.OS.Network.Handler
    {
        public class TxReceivedEventHandler : ILocalEventHandler<TxReceivedEventData>
        {
            public IOptionsSnapshot<ChainOptions> ChainOptions { get; set; }

            public IChainRelatedComponentManager<ITxHub> TxHubs { get; set; }

            private int ChainId => ChainOptions.Value.ChainId;

            public async Task HandleEventAsync(TxReceivedEventData eventData)
            {
                await TxHubs.Get(ChainId).AddTransactionAsync(ChainId, eventData.Transaction);
            }
        }
    }
}