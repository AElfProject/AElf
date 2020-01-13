using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IExecutiveService
    {
        ConcurrentBag<IExecutive> GetPool(Address address, Hash codeHash);
        void PutExecutive(Address address, IExecutive executive);
        void ClearExecutives(Address address, IEnumerable<Hash> codeHashes);
        void CleanIdleExecutive();
        Task<IExecutive> GetExecutiveAsync(SmartContractRegistration smartContractRegistration);
    }
    
    public class ExecutiveService : IExecutiveService, ITransientDependency
    {
        private readonly ISmartContractRunnerContainer _smartContractRunnerContainer;
        private readonly IHostSmartContractBridgeContextService _hostSmartContractBridgeContextService;
        private readonly IExecutiveProvider _executiveProvider;

        public ExecutiveService(ISmartContractRunnerContainer smartContractRunnerContainer, 
            IHostSmartContractBridgeContextService hostSmartContractBridgeContextService, 
            IExecutiveProvider executiveProvider)
        {
            _smartContractRunnerContainer = smartContractRunnerContainer;
            _hostSmartContractBridgeContextService = hostSmartContractBridgeContextService;
            _executiveProvider = executiveProvider;
        }

        public ConcurrentBag<IExecutive> GetPool(Address address, Hash codeHash)
        {
            return _executiveProvider.GetPool(address, codeHash);
        }

        public void PutExecutive(Address address, IExecutive executive)
        {
            _executiveProvider.PutExecutive(address, executive);
        }

        public void ClearExecutives(Address address, IEnumerable<Hash> codeHashes)
        {
            _executiveProvider.ClearExecutives(address,codeHashes);
        }

        public void CleanIdleExecutive()
        {
            _executiveProvider.CleanIdleExecutive();
        }

        public async Task<IExecutive> GetExecutiveAsync(SmartContractRegistration smartContractRegistration)
        {
            // get runner
            var runner = _smartContractRunnerContainer.GetRunner(smartContractRegistration.Category);

            // run smartContract executive info and return executive
            var executive = await runner.RunAsync(smartContractRegistration);

            var context =
                _hostSmartContractBridgeContextService.Create();
            executive.SetHostSmartContractBridgeContext(context);
            return executive;
        }
    }
}