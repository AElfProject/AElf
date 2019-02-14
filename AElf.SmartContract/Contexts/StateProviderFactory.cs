using System;
using AElf.Kernel.SmartContractExecution.Domain;

namespace AElf.SmartContract.Contexts
{
    public class StateProviderFactory : IStateProviderFactory
    {
        private readonly IBlockchainStateManager _blockchainStateManager;
        private readonly IStateManager _stateManager;

        public StateProviderFactory(IBlockchainStateManager blockchainStateManager, IStateManager stateManager)
        {
            _blockchainStateManager = blockchainStateManager;
            _stateManager = stateManager;
        }

        public IStateProvider CreateStateProvider()
        {
            return new StateProvider()
            {
                BlockchainStateManager = _blockchainStateManager
            };
        }

        public IStateManager CreateStateManager()
        {
            return _stateManager;
        }
    }
}