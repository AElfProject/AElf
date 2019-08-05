using AElf.Kernel.SmartContract.Application;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken.Messages;

namespace AElf.Kernel.TransactionPool.Application
{
    internal interface ITokenContractReaderFactory
    {
        TokenContractContainer.TokenContractStub Create(IChainContext chainContext);
    }

    internal class TokenContractReaderFactory : ITokenContractReaderFactory
    {
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly ISmartContractAddressService _smartContractAddressService;

        public TokenContractReaderFactory(ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            ISmartContractAddressService smartContractAddressService)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _smartContractAddressService = smartContractAddressService;
        }

        public TokenContractContainer.TokenContractStub Create(IChainContext chainContext)
        {
            return new TokenContractContainer.TokenContractStub
            {
                __factory = new TokenContractMethodStubFactory(_transactionReadOnlyExecutionService,
                    _smartContractAddressService,
                    chainContext)
            };
        }
    }
}