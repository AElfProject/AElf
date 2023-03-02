using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.CodeCheck.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;

namespace AElf.OS;

public class TestingSmartContractExecutiveService : SmartContractExecutiveService
{
    public TestingSmartContractExecutiveService(
        IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider,
        ISmartContractRunnerContainer smartContractRunnerContainer,
        IHostSmartContractBridgeContextService hostSmartContractBridgeContextService,
        ISmartContractRegistrationProvider smartContractRegistrationProvider,
        ISmartContractExecutiveProvider smartContractExecutiveProvider,
        ITransactionContextFactory transactionContextFactory,
        ISmartContractCodeService smartContractCodeService,
        ICodePatchService codePatchService)
        : base(defaultContractZeroCodeProvider,
            smartContractRunnerContainer,
            hostSmartContractBridgeContextService,
            smartContractRegistrationProvider,
            smartContractExecutiveProvider,
            transactionContextFactory,
            smartContractCodeService,
            codePatchService
        )
    {
    }
}