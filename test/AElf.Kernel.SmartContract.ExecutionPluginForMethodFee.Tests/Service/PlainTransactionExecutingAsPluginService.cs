using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.FeatureDisable.Core;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests.Service;

public class PlainTransactionExecutingAsPluginService : PlainTransactionExecutingService
{
    // for sending transaction
    private readonly Hash _pluginOriginId = new();
    private readonly ISmartContractExecutiveService _smartContractExecutiveService;

    public PlainTransactionExecutingAsPluginService
    (ISmartContractExecutiveService smartContractExecutiveService,
        IEnumerable<IPostExecutionPlugin> postPlugins, IEnumerable<IPreExecutionPlugin> prePlugins,
        ITransactionContextFactory transactionContextFactory, IFeatureDisableService featureDisableService) : base(
        smartContractExecutiveService, postPlugins, prePlugins, transactionContextFactory, featureDisableService)
    {
        _smartContractExecutiveService = smartContractExecutiveService;
    }

    protected override async Task<TransactionTrace> ExecuteOneAsync(
        SingleTransactionExecutingDto singleTxExecutingDto,
        CancellationToken cancellationToken)
    {
        if (singleTxExecutingDto.IsCancellable)
            cancellationToken.ThrowIfCancellationRequested();

        singleTxExecutingDto.OriginTransactionId = _pluginOriginId;
        var txContext = CreateTransactionContext(singleTxExecutingDto);
        var trace = txContext.Trace;

        var internalStateCache = new TieredStateCache(singleTxExecutingDto.ChainContext.StateCache);
        var internalChainContext =
            new ChainContextWithTieredStateCache(singleTxExecutingDto.ChainContext, internalStateCache);

        IExecutive executive;
        try
        {
            executive = await _smartContractExecutiveService.GetExecutiveAsync(
                internalChainContext,
                singleTxExecutingDto.Transaction.To);
        }
        catch (SmartContractFindRegistrationException)
        {
            txContext.Trace.ExecutionStatus = ExecutionStatus.ContractError;
            txContext.Trace.Error += "Invalid contract address.\n";
            return trace;
        }

        try
        {
            await executive.ApplyAsync(txContext);

            if (txContext.Trace.IsSuccessful())
                await ExecuteInlineTransactions(singleTxExecutingDto.Depth, singleTxExecutingDto.CurrentBlockTime,
                    txContext, internalStateCache,
                    internalChainContext,
                    singleTxExecutingDto.OriginTransactionId,
                    cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Transaction execution failed.");
            txContext.Trace.ExecutionStatus = ExecutionStatus.ContractError;
            txContext.Trace.Error += ex + "\n";
            throw;
        }
        finally
        {
            await _smartContractExecutiveService.PutExecutiveAsync(singleTxExecutingDto.ChainContext,
                singleTxExecutingDto.Transaction.To, executive);
        }

        return trace;
    }

    private async Task ExecuteInlineTransactions(int depth, Timestamp currentBlockTime,
        ITransactionContext txContext, TieredStateCache internalStateCache,
        IChainContext internalChainContext,
        Hash originTransactionId,
        CancellationToken cancellationToken)
    {
        var trace = txContext.Trace;
        internalStateCache.Update(txContext.Trace.GetStateSets());
        foreach (var inlineTx in txContext.Trace.InlineTransactions)
        {
            var singleTxExecutingDto = new SingleTransactionExecutingDto
            {
                Depth = depth + 1,
                ChainContext = internalChainContext,
                Transaction = inlineTx,
                CurrentBlockTime = currentBlockTime,
                Origin = txContext.Origin,
                OriginTransactionId = originTransactionId
            };

            var inlineTrace = await ExecuteOneAsync(singleTxExecutingDto, cancellationToken);

            if (inlineTrace == null)
                break;
            trace.InlineTraces.Add(inlineTrace);
            if (!inlineTrace.IsSuccessful())
                // Already failed, no need to execute remaining inline transactions
                break;

            internalStateCache.Update(inlineTrace.GetStateSets());
        }
    }
}