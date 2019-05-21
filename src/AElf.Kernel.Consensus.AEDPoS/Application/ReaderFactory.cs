using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Kernel.Consensus.AEDPoS
{
    internal interface IAEDPoSReaderFactory
    {
        AEDPoSContractContainer.AEDPoSContractStub Create(IChainContext chainContext);
    }

    internal class AEDPoSReaderFactory : IAEDPoSReaderFactory
    {
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IBlockTimeProvider _blockTimeProvider;

        public AEDPoSReaderFactory(ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            ISmartContractAddressService smartContractAddressService, IBlockTimeProvider blockTimeProvider)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _smartContractAddressService = smartContractAddressService;
            _blockTimeProvider = blockTimeProvider;
        }

        public AEDPoSContractContainer.AEDPoSContractStub Create(IChainContext chainContext)
        {
            return new AEDPoSContractContainer.AEDPoSContractStub
            {
                __factory = new MethodStubFactory(_transactionReadOnlyExecutionService,
                    _smartContractAddressService,
                    chainContext,
                    _blockTimeProvider)
            };
        }
    }
}