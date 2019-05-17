using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Kernel.SmartContract.Application;

namespace AElf.Kernel.Consensus.AEDPoS
{
    internal interface IReaderFactory
    {
        AEDPoSContractContainer.AEDPoSContractStub Create(IChainContext chainContext);
        AEDPoSContractContainer.AEDPoSContractStub Create(Hash blockHash, long blockHeight);
    }

    internal class ReaderFactory : IReaderFactory
    {
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IBlockTimeProvider _blockTimeProvider;

        public ReaderFactory(ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
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

        public AEDPoSContractContainer.AEDPoSContractStub Create(Hash blockHash, long blockHeight)
        {
            return Create(new ChainContext
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight
            });
        }
    }
}