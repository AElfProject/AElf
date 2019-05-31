using AElf.Kernel.SmartContract.Application;
using Acs4;

namespace AElf.Kernel.Consensus.Application
{
    internal interface IConsensusReaderFactory
    {
        ConsensusContractContainer.ConsensusContractStub Create(IChainContext chainContext);
    }

    internal class ConsensusReaderFactory : IConsensusReaderFactory
    {
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IBlockTimeProvider _blockTimeProvider;

        public ConsensusReaderFactory(ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            ISmartContractAddressService smartContractAddressService, IBlockTimeProvider blockTimeProvider)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _smartContractAddressService = smartContractAddressService;
            _blockTimeProvider = blockTimeProvider;
        }

        public ConsensusContractContainer.ConsensusContractStub Create(IChainContext chainContext)
        {
            return new ConsensusContractContainer.ConsensusContractStub
            {
                __factory = new MethodStubFactory(_transactionReadOnlyExecutionService,
                    _smartContractAddressService,
                    chainContext,
                    _blockTimeProvider)
            };
        }
    }
}