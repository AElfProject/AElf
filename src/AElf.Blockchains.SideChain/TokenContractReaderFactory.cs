using AElf.Kernel.SmartContract.Application;
using AElf.Contracts.MultiToken;
using AElf.Kernel;
using AElf.Kernel.TransactionPool.Application;

namespace AElf.Blockchains.SideChain
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