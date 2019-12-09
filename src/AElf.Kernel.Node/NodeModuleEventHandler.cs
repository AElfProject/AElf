using System.Threading.Tasks;
using AElf.Kernel.Node.Events;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Node
{
    internal class NodeModuleEventHandler : ILocalEventHandler<InitialSyncFinishedEvent>, ITransientDependency
    {
        private readonly ICodeCheckService _codeCheckService;
            
        public NodeModuleEventHandler(ICodeCheckService codeCheckService)
        {
            _codeCheckService = codeCheckService;
        }
        
        public Task HandleEventAsync(InitialSyncFinishedEvent eventData)
        {
            // Activate code check service in smart contract module
            _codeCheckService.Enable();
            return Task.CompletedTask;
        }
    }
}
