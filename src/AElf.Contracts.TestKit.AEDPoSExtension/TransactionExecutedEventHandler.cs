using System.Threading.Tasks;
using AElf.Kernel.SmartContractExecution.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Contracts.TestKet.AEDPoSExtension
{
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
}