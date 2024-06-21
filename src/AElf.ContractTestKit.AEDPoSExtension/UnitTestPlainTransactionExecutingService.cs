using System.Collections.Generic;
using AElf.Kernel.SmartContract.Application;

namespace AElf.ContractTestKit.AEDPoSExtension;

public class UnitTestPlainTransactionExecutingService : PlainTransactionExecutingService
{
    public UnitTestPlainTransactionExecutingService(ISmartContractExecutiveService smartContractExecutiveService,
        IEnumerable<IPostExecutionPlugin> postPlugins, IEnumerable<IPreExecutionPlugin> prePlugins,
        ITransactionContextFactory transactionContextFactory) : base(smartContractExecutiveService, postPlugins,
        prePlugins, transactionContextFactory)
    {
    }
}