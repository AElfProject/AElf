using System.Threading.Tasks;
using AElf.Kernel.Events;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContract
{
    internal class SmartContractModuleEventHandler : ILocalEventHandler<InitialSyncFinishedEvent>, ITransientDependency
    {
        private readonly ICodeCheckActivationService _codeCheckActivationService;
            
        public SmartContractModuleEventHandler(ICodeCheckActivationService codeCheckActivationService)
        {
            _codeCheckActivationService = codeCheckActivationService;
        }
        
        public async Task HandleEventAsync(InitialSyncFinishedEvent eventData)
        {
            _codeCheckActivationService.Enable();
        }
    }
}
