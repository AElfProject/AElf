using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.TestContract.BasicFunctionWithParallel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Volo.Abp.DependencyInjection;

namespace AElf.Parallel.Tests;

public class DeleteDataFromStateDbPostExecutionPlugin : IPostExecutionPlugin, ISingletonDependency
{
    private readonly IHostSmartContractBridgeContextService _contextService;

    public DeleteDataFromStateDbPostExecutionPlugin(IHostSmartContractBridgeContextService contextService)
    {
        _contextService = contextService;
    }

    public Task<IEnumerable<Transaction>> GetPostTransactionsAsync(IReadOnlyList<ServiceDescriptor> descriptors,
        ITransactionContext transactionContext)
    {
        if (!descriptors.Any(service => service.File.Name == "test_basic_function_with_parallel_contract.proto"))
            return Task.FromResult(new List<Transaction>().AsEnumerable());

        if (transactionContext.Transaction.To == ParallelTestHelper.BasicFunctionWithParallelContractAddress &&
            !transactionContext.Transaction.MethodName.EndsWith("Plugin"))
            return Task.FromResult(new List<Transaction>().AsEnumerable());

        var context = _contextService.Create();
        context.TransactionContext = transactionContext;

        var transactions = new List<Transaction>();
        switch (transactionContext.Transaction.MethodName)
        {
            case nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                .RemoveValueFromInlineWithPlugin):
            case nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                .RemoveValueFromPrePlugin):
            case nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                .RemoveValueWithPlugin):
            {
                var input = RemoveValueInput.Parser.ParseFrom(transactionContext.Transaction.Params);
                transactions.Add(new Transaction
                {
                    From = transactionContext.Transaction.From,
                    To = ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                    Params = new IncreaseValueInput
                    {
                        Key = input.Key,
                        Memo = Guid.NewGuid().ToString()
                    }.ToByteString(),
                    MethodName = nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                        .IncreaseValue),
                    RefBlockNumber = transactionContext.BlockHeight - 1,
                    RefBlockPrefix =
                        BlockHelper.GetRefBlockPrefix(transactionContext.PreviousBlockHash)
                });
                break;
            }

            case nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                .RemoveValueFromPostPlugin):
            case nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                .RemoveValueParallelFromPostPlugin):
            {
                var input = RemoveValueInput.Parser.ParseFrom(transactionContext.Transaction.Params);
                transactions.Add(new Transaction
                {
                    From = transactionContext.Transaction.From,
                    To = ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                    Params = input.ToByteString(),
                    MethodName = nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                        .RemoveValue),
                    RefBlockNumber = transactionContext.BlockHeight - 1,
                    RefBlockPrefix =
                        BlockHelper.GetRefBlockPrefix(transactionContext.PreviousBlockHash)
                });
                break;
            }

            case nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                .IncreaseValueWithPostPlugin):
            case nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                .IncreaseValueWithInlineAndPostPlugin):
            case nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                .IncreaseValueWithPlugin):
            case nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                .IncreaseValueWithInlineAndPlugin):
            case nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                .IncreaseValueParallelWithInlineAndPlugin):
            case nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                .IncreaseValueFailedWithPlugin):
            case nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                .IncreaseValueFailedWithPostPlugin):
            case nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                .IncreaseValueFailedWithInlineAndPlugin):
            case nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                .IncreaseValueFailedWithInlineAndPostPlugin):
            case nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                .IncreaseValueFailedParallelWithInlineAndPlugin):
            case nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                .IncreaseValueWithFailedInlineAndPlugin):
            case nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                .IncreaseValueWithFailedInlineAndPostPlugin):
            case nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                .IncreaseValueParallelWithFailedInlineAndPlugin):
            case nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                .IncreaseValueWithFailedPrePluginAndPostPlugin):
            case nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                .IncreaseValueWithInlineAndFailedPrePluginAndPostPlugin):
            {
                var input = IncreaseValueInput.Parser.ParseFrom(transactionContext.Transaction.Params);
                transactions.Add(new Transaction
                {
                    From = transactionContext.Transaction.From,
                    To = ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                    Params = new IncreaseValueInput
                    {
                        Key = input.Key,
                        Memo = Guid.NewGuid().ToString()
                    }.ToByteString(),
                    MethodName = nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                        .IncreaseValue),
                    RefBlockNumber = transactionContext.BlockHeight - 1,
                    RefBlockPrefix =
                        BlockHelper.GetRefBlockPrefix(transactionContext.PreviousBlockHash)
                });
                break;
            }

            case nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                .IncreaseValueWithFailedPostPlugin):
            case nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                .IncreaseValueWithInlineAndFailedPostPlugin):
            case nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                .IncreaseValueWithPrePluginAndFailedPostPlugin):
            case nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                .IncreaseValueWithInlineAndPrePluginAndFailedPostPlugin):
            {
                var input = IncreaseValueInput.Parser.ParseFrom(transactionContext.Transaction.Params);
                transactions.Add(new Transaction
                {
                    From = transactionContext.Transaction.From,
                    To = ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                    Params = new IncreaseValueInput
                    {
                        Key = input.Key,
                        Memo = Guid.NewGuid().ToString()
                    }.ToByteString(),
                    MethodName = nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                        .IncreaseValueFailed),
                    RefBlockNumber = transactionContext.BlockHeight - 1,
                    RefBlockPrefix =
                        BlockHelper.GetRefBlockPrefix(transactionContext.PreviousBlockHash)
                });
                break;
            }
        }

        return Task.FromResult(transactions.AsEnumerable());
    }
}