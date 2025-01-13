using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.FeatureDisable.Core;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Events;

namespace AElf.ContractTestKit.AEDPoSExtension;

public class UnitTestPlainTransactionExecutingService : PlainTransactionExecutingService
{
    public UnitTestPlainTransactionExecutingService(ISmartContractExecutiveService smartContractExecutiveService,
        IEnumerable<IPostExecutionPlugin> postPlugins, IEnumerable<IPreExecutionPlugin> prePlugins,
        ITransactionContextFactory transactionContextFactory, IFeatureDisableService featureDisableService) : base(
        smartContractExecutiveService, postPlugins, prePlugins, transactionContextFactory, featureDisableService)
    {
    }

    protected override async Task<TransactionTrace> ExecuteOneAsync(SingleTransactionExecutingDto singleTxExecutingDto,
        CancellationToken cancellationToken)
    {
        TransactionTrace trace = null;
        try
        {
            trace = await base.ExecuteOneAsync(singleTxExecutingDto, cancellationToken);
        }
        finally
        {
            await LocalEventBus.PublishAsync(new TransactionExecutedEventData
            {
                TransactionTrace = trace
            });
        }

        return trace;
    }
}