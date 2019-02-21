using AElf.Kernel.SmartContractExecution.Domain;
using AElf.Kernel.SmartContract.Contexts;

namespace AElf.Sdk.CSharp2.Tests
{
    public class MockStateProviderFactory : IStateProviderFactory
    {
        private readonly IStateManager _stateManager;

        public MockStateProviderFactory(IStateManager stateManager)
        {
            _stateManager = stateManager;
        }

        public IStateProvider CreateStateProvider()
        {
            return new MockStateProvider(_stateManager);
        }

        public IStateManager CreateStateManager()
        {
            return _stateManager;
        }
    }
}