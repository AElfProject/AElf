using System;
using System.Threading;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests.Service;

public partial class PlainTransactionExecutingAsPluginService
{
    protected async virtual Task<FlowBehavior> HandleExceptionWhileGettingExecutive(SmartContractFindRegistrationException ex,
        IChainContext internalChainContext, Address contractAddress, ITransactionContext txContext)
    {
        txContext.Trace.ExecutionStatus = ExecutionStatus.ContractError;
        txContext.Trace.Error += "Invalid contract address.\n";
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = null
        };
    }

    protected async virtual Task<FlowBehavior> HandleExceptionWhileApplyingExecutive(Exception ex, IExecutive executive,
        ITransactionContext txContext, SingleTransactionExecutingDto singleTxExecutingDto,
        TieredStateCache internalStateCache, IChainContext internalChainContext, CancellationToken cancellationToken)
    {
        Logger.LogError(ex, "Transaction execution failed.");
        txContext.Trace.ExecutionStatus = ExecutionStatus.ContractError;
        txContext.Trace.Error += ex + "\n";

        await _smartContractExecutiveService.PutExecutiveAsync(singleTxExecutingDto.ChainContext,
            singleTxExecutingDto.Transaction.To, executive);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = txContext.Trace
        };
    }
}