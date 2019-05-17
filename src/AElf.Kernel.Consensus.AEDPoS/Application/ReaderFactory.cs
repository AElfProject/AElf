using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContract.Application;

namespace AElf.Kernel.Consensus.AEDPoS
{
    internal interface IAEDPoSReaderFactory
    {
        AEDPoSContractImplContainer.AEDPoSContractImplStub Create(IChainContext chainContext);
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

        public AEDPoSContractImplContainer.AEDPoSContractImplStub Create(IChainContext chainContext)
        {
            return new AEDPoSContractImplContainer.AEDPoSContractImplStub
            {
                __factory = new MethodStubFactory(_transactionReadOnlyExecutionService,
                    _smartContractAddressService,
                    chainContext,
                    _blockTimeProvider)
            };
        }
    }
}