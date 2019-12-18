using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContract.Application;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.Consensus.AEDPoS
{
    internal interface IConsensusReaderFactory
    {
        AEDPoSContractContainer.AEDPoSContractStub Create(IChainContext chainContext);
    }

    internal class ConsensusReaderFactory : IConsensusReaderFactory
    {
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IConsensusReaderContextService _contextService;

        public ConsensusReaderFactory(ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
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