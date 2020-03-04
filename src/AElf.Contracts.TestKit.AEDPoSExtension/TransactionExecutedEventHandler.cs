using System.Threading.Tasks;
using AElf.Kernel.SmartContract;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Contracts.TestKet.AEDPoSExtension
{
#if DEBUG
    public class TransactionExecutedEventHandler : ILocalEventHandler<TransactionExecutedEventData>,
        ITransientDependency
    {
        private readonly ITransactionTraceProvider _traceProvider;

        public TransactionExecutedEventHandler(ITransactionTraceProvider traceProvider)
        {
            _traceProvider = traceProvider;
        }

        public Task HandleEventAsync(TransactionExecutedEventData eventData)
        {
            _traceProvider.AddTransactionTrace(eventData.TransactionTrace);
            return Task.CompletedTask;
        }
    }
#endif
}