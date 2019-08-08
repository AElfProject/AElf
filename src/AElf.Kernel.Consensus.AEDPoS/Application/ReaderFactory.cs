using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContract.Application;

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
        private readonly IConsensusReaderContextService _contextService;

        public AEDPoSReaderFactory(ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            ISmartContractAddressService smartContractAddressService, IConsensusReaderContextService contextService)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _smartContractAddressService = smartContractAddressService;
            _contextService = contextService;
        }

        public AEDPoSContractContainer.AEDPoSContractStub Create(IChainContext chainContext)
        {
            return new AEDPoSContractContainer.AEDPoSContractStub
            {
                __factory = new MethodStubFactory(_transactionReadOnlyExecutionService,
                    _smartContractAddressService,
                    chainContext,
                    _contextService)
            };
        }
    }
}