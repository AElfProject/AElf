using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.Infrastructure;

namespace AElf.OS
{
    public class TestingSmartContractExecutiveService : SmartContractExecutiveService
    {
        public TestingSmartContractExecutiveService(IDeployedContractAddressProvider deployedContractAddressProvider,
            IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider,
            ISmartContractRunnerContainer smartContractRunnerContainer,
            IHostSmartContractBridgeContextService hostSmartContractBridgeContextService,
            IChainBlockLinkService chainBlockLinkService, IBlockchainService blockchainService, 
            ISmartContractRegistrationCacheProvider smartContractRegistrationCacheProvider,
            ISmartContractExecutiveProvider smartContractExecutiveProvider) : base(deployedContractAddressProvider,
            defaultContractZeroCodeProvider,
            smartContractRunnerContainer,
            hostSmartContractBridgeContextService,
            chainBlockLinkService,
            blockchainService,
            smartContractRegistrationCacheProvider,
            smartContractExecutiveProvider)
        {
        }
    }
}