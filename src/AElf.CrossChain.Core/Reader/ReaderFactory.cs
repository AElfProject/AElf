using AElf.Contracts.CrossChain;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.CrossChain
{
    internal interface IReaderFactory
    {
        CrossChainContractContainer.CrossChainContractStub Create(IChainContext chainContext);
        CrossChainContractContainer.CrossChainContractStub Create(Hash blockHash, long blockHeight);
    }

    internal class ReaderFactory : IReaderFactory
    {
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly ISmartContractAddressService _smartContractAddressService;

        public ReaderFactory(ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            ISmartContractAddressService smartContractAddressService)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _smartContractAddressService = smartContractAddressService;
        }

        public CrossChainContractContainer.CrossChainContractStub Create(IChainContext chainContext)
        {
            return new CrossChainContractContainer.CrossChainContractStub()
            {
                __factory = new MethodStubFactory(_transactionReadOnlyExecutionService, _smartContractAddressService,
                    chainContext)
            };
        }

        public CrossChainContractContainer.CrossChainContractStub Create(Hash blockHash, long blockHeight)
        {
            return Create(new ChainContext()
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight
            });
        }
    }
}