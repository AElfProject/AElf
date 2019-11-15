using System.Threading.Tasks;
using AElf.Kernel.Events;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContract
{
    internal class SmartContractModuleEventHandler : ILocalEventHandler<InitialSyncFinishedEvent>, ITransientDependency
    {
        private readonly ICodeCheckService _codeCheckService;
            
        public SmartContractModuleEventHandler(ICodeCheckService codeCheckService)
        {
            _codeCheckService = codeCheckService;
        }
        
        public async Task HandleEventAsync(InitialSyncFinishedEvent eventData)
        {
            _codeCheckService.Enable();
        }
    }
}
