using System;
using AElf.Kernel.SmartContractExecution.Domain;

namespace AElf.SmartContract.Contexts
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
            return new StateProvider()
            {
                BlockchainStateManager = _blockchainStateManager
            };
        }

    }
}