using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.ChainController.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContractExecution.Application;

namespace AElf.Kernel.Node.Application
{
    public interface IBlockchainNodeContextProxyService
    {
        IBlockchainService BlockchainService { get; }
        IChainCreationService ChainCreationService { get; }
        ISmartContractAddressUpdateService SmartContractAddressUpdateService { get; }
        IConsensusService ConsensusService { get; }
    }

    public class BlockchainNodeContextProxyService : IBlockchainNodeContextProxyService
    {
        public IBlockchainService BlockchainService { get; }
        public IChainCreationService ChainCreationService { get; }
        public ISmartContractAddressUpdateService SmartContractAddressUpdateService { get; }
        public IConsensusService ConsensusService { get; }


        public BlockchainNodeContextProxyService(IBlockchainService blockchainService,
            IChainCreationService chainCreationService,
            ISmartContractAddressUpdateService smartContractAddressUpdateService,
            IConsensusService consensusService)
        {
            BlockchainService = blockchainService;
            ChainCreationService = chainCreationService;
            SmartContractAddressUpdateService = smartContractAddressUpdateService;
            ConsensusService = consensusService;
        }
    }
}