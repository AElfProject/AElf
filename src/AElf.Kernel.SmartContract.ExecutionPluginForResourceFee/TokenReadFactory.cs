using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee
{
    internal interface ITokenReaderFactory
    {
        TokenContractImplContainer.TokenContractImplStub Create(Hash blockHash, long blockHeight,
            Timestamp timestamp = null);
    }

    internal class TokenReaderFactory : ITokenReaderFactory
    {
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly ISmartContractAddressService _smartContractAddressService;

        public TokenReaderFactory(ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            ISmartContractAddressService smartContractAddressService)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _smartContractAddressService = smartContractAddressService;
        }

        public TokenContractImplContainer.TokenContractImplStub Create(Hash blockHash, long blockHeight,
            Timestamp timestamp = null)
        {
            return new TokenContractImplContainer.TokenContractImplStub()
            {
                __factory = new MethodStubFactory(_transactionReadOnlyExecutionService, _smartContractAddressService,
                    new ChainContext
                    {
                        BlockHash = blockHash,
                        BlockHeight = blockHeight
                    }, timestamp ?? TimestampHelper.GetUtcNow())
            };
        }
    }
}