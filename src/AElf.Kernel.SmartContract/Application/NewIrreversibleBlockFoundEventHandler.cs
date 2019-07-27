using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContract.Application
{
    public class NewIrreversibleBlockFoundEventHandler : ILocalEventHandler<NewIrreversibleBlockFoundEvent>, ITransientDependency
    {
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;

        public NewIrreversibleBlockFoundEventHandler(ISmartContractExecutiveService smartContractExecutiveService)
        {
            _smartContractExecutiveService = smartContractExecutiveService;
        }
        
        public Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            _smartContractExecutiveService.ClearUpdateContractInfo(eventData.BlockHeight);
            return Task.CompletedTask;
        }
    }
}