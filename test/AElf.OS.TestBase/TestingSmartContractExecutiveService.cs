using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;

namespace AElf.OS
{
    public class TestingSmartContractExecutiveService : SmartContractExecutiveService
    {
        public TestingSmartContractExecutiveService(IDeployedContractAddressProvider deployedContractAddressProvider,
            IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider,
            IBlockchainService blockchainService,
            IExecutiveService executiveService,
            ISmartContractRegistrationService smartContractRegistrationService) : base(deployedContractAddressProvider,
            defaultContractZeroCodeProvider, blockchainService, executiveService, smartContractRegistrationService)
        {
        }
    }
}