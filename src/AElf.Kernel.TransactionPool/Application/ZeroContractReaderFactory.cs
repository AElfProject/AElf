using AElf.Kernel.SmartContract.Application;
using AElf.Contracts.Genesis;

namespace AElf.Kernel.TransactionPool.Application
{
    internal interface IZeroContractReaderFactory
    {
        BasicContractZeroContainer.BasicContractZeroStub Create(IChainContext chainContext);
    }

    internal class ZeroContractReaderFactory : IZeroContractReaderFactory
    {
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly ISmartContractAddressService _smartContractAddressService;

        public ZeroContractReaderFactory(ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            ISmartContractAddressService smartContractAddressService)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _smartContractAddressService = smartContractAddressService;
        }

        public BasicContractZeroContainer.BasicContractZeroStub Create(IChainContext chainContext)
        {
            return new BasicContractZeroContainer.BasicContractZeroStub
            {
                __factory = new ZeroContractMethodStubFactory(_transactionReadOnlyExecutionService,
                    _smartContractAddressService,
                    chainContext)
            };
        }
    }
}