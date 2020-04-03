using AElf.Kernel.SmartContract.Application;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    //TODO: base class, should not know token

    internal interface ITokenContractReaderFactory
    {
        TokenContractImplContainer.TokenContractImplStub Create(IChainContext chainContext);
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

        public TokenContractImplContainer.TokenContractImplStub Create(IChainContext chainContext)
        {
            return new TokenContractImplContainer.TokenContractImplStub
            {
                __factory = new TokenContractMethodStubFactory(_transactionReadOnlyExecutionService,
                    _smartContractAddressService,
                    chainContext)
            };
        }
    }
}