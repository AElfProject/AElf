using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
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

            public ITxHub TxHub { get; set; }

            private int ChainId
            {
                get { return ChainOptions.Value.ChainId.ConvertBase58ToChainId(); }
            }

            public async Task HandleEventAsync(TxReceivedEventData eventData)
            {
                await TxHub.AddTransactionAsync(ChainId, eventData.Transaction);
            }
        }
    }
}