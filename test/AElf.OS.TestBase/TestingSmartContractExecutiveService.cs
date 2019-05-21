using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Sdk;

namespace AElf.OS
{
    public class TestingSmartContractExecutiveService : SmartContractExecutiveService
    {
        public TestingSmartContractExecutiveService(ISmartContractRunnerContainer smartContractRunnerContainer,
            IStateProviderFactory stateProviderFactory,
            IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider,
            IHostSmartContractBridgeContextService hostSmartContractBridgeContextService) : base(
            smartContractRunnerContainer, stateProviderFactory, defaultContractZeroCodeProvider,
            hostSmartContractBridgeContextService)
        {
        }

        public override Task PutExecutiveAsync(Address address, IExecutive executive)
        {
            if (_executivePools.TryGetValue(address, out var pool))
            {
                pool.Add(executive);
            }

            return Task.CompletedTask;
        }
    }
}