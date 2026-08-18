using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContract.Application;

public class TransactionReadOnlyExecutionService : ITransactionReadOnlyExecutionService
{
    private readonly ISmartContractExecutiveService _smartContractExecutiveService;
    private readonly ISyntheticTransactionExecutionProvider _syntheticTransactionExecutionProvider;
    private readonly ITransactionContextFactory _transactionContextFactory;

    public TransactionReadOnlyExecutionService(ISmartContractExecutiveService smartContractExecutiveService,
        ITransactionContextFactory transactionContextFactory,
        ISyntheticTransactionExecutionProvider syntheticTransactionExecutionProvider)
    {
        _smartContractExecutiveService = smartContractExecutiveService;
        _transactionContextFactory = transactionContextFactory;
        _syntheticTransactionExecutionProvider = syntheticTransactionExecutionProvider;
        Logger = NullLogger<TransactionReadOnlyExecutionService>.Instance;
    }

    public ILogger<TransactionReadOnlyExecutionService> Logger { get; set; }

    public async Task<TransactionTrace> ExecuteAsync(IChainContext chainContext, Transaction transaction,
        Timestamp currentBlockTime)
    {
        var transactionContext = _transactionContextFactory.Create(transaction, chainContext, currentBlockTime);
        if (_syntheticTransactionExecutionProvider.TryApply(transaction, transactionContext.Trace))
        {
            Logger.LogDebug("Read-only transaction {TransactionId} to contract {ContractAddress} was synthetically executed.",
                transactionContext.Trace.TransactionId, transaction.To);
            return transactionContext.Trace;
        }
        var executive = await _smartContractExecutiveService.GetExecutiveAsync(
            chainContext, transaction.To);

        try
        {
            await executive.ApplyAsync(transactionContext);
        }
        finally
        {
            await _smartContractExecutiveService.PutExecutiveAsync(chainContext, transaction.To, executive);
        }

        return transactionContext.Trace;
    }

    public async Task<byte[]> GetFileDescriptorSetAsync(IChainContext chainContext, Address address)
    {
        IExecutive executive = null;

        byte[] output;
        try
        {
            executive = await _smartContractExecutiveService.GetExecutiveAsync(
                chainContext, address);
            output = executive.GetFileDescriptorSet();
        }
        finally
        {
            if (executive != null)
                await _smartContractExecutiveService.PutExecutiveAsync(chainContext, address, executive);
        }

        return output;
    }

    public async Task<IEnumerable<FileDescriptor>> GetFileDescriptorsAsync(IChainContext chainContext, Address address)
    {
        IExecutive executive = null;

        IEnumerable<FileDescriptor> output;
        try
        {
            executive = await _smartContractExecutiveService.GetExecutiveAsync(
                chainContext, address);
            output = executive.GetFileDescriptors();
        }
        finally
        {
            if (executive != null)
                await _smartContractExecutiveService.PutExecutiveAsync(chainContext, address, executive);
        }

        return output;
    }

    public async Task<string> GetTransactionParametersAsync(IChainContext chainContext, Transaction transaction)
    {
        var address = transaction.To;
        IExecutive executive = null;
        try
        {
            executive = await _smartContractExecutiveService.GetExecutiveAsync(chainContext, address);
            return executive.GetJsonStringOfParameters(transaction.MethodName, transaction.Params.ToByteArray());
        }
        finally
        {
            if (executive != null)
                await _smartContractExecutiveService.PutExecutiveAsync(chainContext, address, executive);
        }
    }

    public async Task<bool> IsViewTransactionAsync(IChainContext chainContext, Transaction transaction)
    {
        var address = transaction.To;
        IExecutive executive = null;
        try
        {
            executive = await _smartContractExecutiveService.GetExecutiveAsync(chainContext, address);
            return executive.IsView(transaction.MethodName);
        }
        finally
        {
            if (executive != null)
                await _smartContractExecutiveService.PutExecutiveAsync(chainContext, address, executive);
        }
    }
}
