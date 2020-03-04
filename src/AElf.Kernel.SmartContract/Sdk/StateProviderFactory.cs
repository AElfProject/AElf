using AElf.Kernel.SmartContract.Domain;

namespace AElf.Kernel.SmartContract
{
    public class StateProviderFactory : IStateProviderFactory
    {
        private readonly IBlockchainStateManager _blockchainStateManager;

        public StateProviderFactory(IBlockchainStateManager blockchainStateManager)
        {
            _blockchainStateManager = blockchainStateManager;
        }

        public IStateProvider CreateStateProvider()
        {
            return new ScopedStateProvider()
            {
            };
        }

    }
}