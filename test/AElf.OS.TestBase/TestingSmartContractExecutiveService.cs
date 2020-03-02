using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.Infrastructure;

namespace AElf.OS
{
    public class TestingSmartContractExecutiveService : SmartContractExecutiveService
    {
        public TestingSmartContractExecutiveService(
            IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider,
            ISmartContractRunnerContainer smartContractRunnerContainer,
            IHostSmartContractBridgeContextService hostSmartContractBridgeContextService,
            IBlockchainStateService blockchainStateService,
            ISmartContractRegistrationCacheProvider smartContractRegistrationCacheProvider,
            ISmartContractExecutiveProvider smartContractExecutiveProvider,
            ISmartContractHeightInfoProvider smartContractHeightInfoProvider) 
            : base(defaultContractZeroCodeProvider,
            smartContractRunnerContainer,
            hostSmartContractBridgeContextService,
            blockchainStateService,
            smartContractRegistrationCacheProvider,
            smartContractExecutiveProvider,
            smartContractHeightInfoProvider
            )
        {
        }
    }
}