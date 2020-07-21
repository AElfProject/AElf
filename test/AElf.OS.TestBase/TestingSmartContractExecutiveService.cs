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
            ISmartContractRegistrationProvider smartContractRegistrationProvider,
            ISmartContractExecutiveProvider smartContractExecutiveProvider,
            ITransactionContextFactory transactionContextFactory)
            : base(defaultContractZeroCodeProvider,
                smartContractRunnerContainer,
                hostSmartContractBridgeContextService,
                smartContractRegistrationProvider,
                smartContractExecutiveProvider,
                transactionContextFactory
            )
        {
        }
    }
}