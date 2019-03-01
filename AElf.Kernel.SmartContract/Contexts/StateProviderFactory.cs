using System;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContractExecution.Domain;

namespace AElf.Kernel.SmartContract.Contexts
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