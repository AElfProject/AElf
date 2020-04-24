using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.TestContract.BasicFunctionWithParallel;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Volo.Abp.DependencyInjection;

namespace AElf.Parallel.Tests
{
    public class DeleteDataFromStateDbPreExecutionPlugin : IPreExecutionPlugin, ISingletonDependency
    {
        private readonly IHostSmartContractBridgeContextService _contextService;

        public DeleteDataFromStateDbPreExecutionPlugin(IHostSmartContractBridgeContextService contextService)
        {
            _contextService = contextService;
        }
        
        public Task<IEnumerable<Transaction>> GetPreTransactionsAsync(IReadOnlyList<ServiceDescriptor> descriptors, ITransactionContext transactionContext)
        {
            if (!descriptors.Any(service => service.File.Name == "test_basic_function_with_parallel_contract.proto"))
            {
                return Task.FromResult(new List<Transaction>().AsEnumerable());
            }
            
            if (transactionContext.Transaction.To == ParallelTestHelper.BasicFunctionWithParallelContractAddress &&
                !transactionContext.Transaction.MethodName.EndsWith("Plugin"))
            {
                return Task.FromResult(new List<Transaction>().AsEnumerable());
            }
            
            var context = _contextService.Create();
            context.TransactionContext = transactionContext;
            
            var transactions = new List<Transaction>();

            switch (transactionContext.Transaction.MethodName)
            {
                case nameof(BasicFunctionWithParallelContract.RemoveValueFromInlineWithPlugin):
                case nameof(BasicFunctionWithParallelContract.RemoveValueFromPostPlugin):
                case nameof(BasicFunctionWithParallelContract.RemoveValueWithPlugin):
                case nameof(BasicFunctionWithParallelContract.RemoveValueParallelFromPostPlugin):
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
                        MethodName = nameof(BasicFunctionWithParallelContract.IncreaseValue),
                        RefBlockNumber = transactionContext.BlockHeight - 1,
                        RefBlockPrefix =
                            BlockHelper.GetRefBlockPrefix(transactionContext.PreviousBlockHash)
                    });
                    break;
                }

                case nameof(BasicFunctionWithParallelContract.RemoveValueFromPrePlugin):
                {
                    var input = RemoveValueInput.Parser.ParseFrom(transactionContext.Transaction.Params);
                    transactions.Add(new Transaction
                    {
                        From = transactionContext.Transaction.From,
                        To = ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        Params = input.ToByteString(),
                        MethodName = nameof(BasicFunctionWithParallelContract.RemoveValue),
                        RefBlockNumber = transactionContext.BlockHeight - 1,
                        RefBlockPrefix =
                            BlockHelper.GetRefBlockPrefix(transactionContext.PreviousBlockHash)
                    });
                    break;
                }
                case nameof(BasicFunctionWithParallelContract.IncreaseValueWithPrePlugin):
                case nameof(BasicFunctionWithParallelContract.IncreaseValueWithInlineAndPrePlugin):
                case nameof(BasicFunctionWithParallelContract.IncreaseValueWithPlugin):
                case nameof(BasicFunctionWithParallelContract.IncreaseValueWithInlineAndPlugin):
                case nameof(BasicFunctionWithParallelContract.IncreaseValueParallelWithInlineAndPlugin):
                case nameof(BasicFunctionWithParallelContract.IncreaseValueFailedWithPlugin): 
                case nameof(BasicFunctionWithParallelContract.IncreaseValueFailedWithPrePlugin):
                case nameof(BasicFunctionWithParallelContract.IncreaseValueFailedWithInlineAndPlugin):
                case nameof(BasicFunctionWithParallelContract.IncreaseValueFailedParallelWithInlineAndPlugin):
                case nameof(BasicFunctionWithParallelContract.IncreaseValueFailedWithInlineAndPrePlugin):
                case nameof(BasicFunctionWithParallelContract.IncreaseValueWithFailedInlineAndPlugin):
                case nameof(BasicFunctionWithParallelContract.IncreaseValueWithFailedInlineAndPrePlugin): 
                case nameof(BasicFunctionWithParallelContract.IncreaseValueParallelWithFailedInlineAndPlugin):
                case nameof(BasicFunctionWithParallelContract.IncreaseValueWithPrePluginAndFailedPostPlugin):
                case nameof(BasicFunctionWithParallelContract.IncreaseValueWithInlineAndPrePluginAndFailedPostPlugin):
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
                        MethodName = nameof(BasicFunctionWithParallelContract.IncreaseValue),
                        RefBlockNumber = transactionContext.BlockHeight - 1,
                        RefBlockPrefix =
                            BlockHelper.GetRefBlockPrefix(transactionContext.PreviousBlockHash)
                    });
                    break;
                }

                case nameof(BasicFunctionWithParallelContract.IncreaseValueWithFailedPrePlugin):
                case nameof(BasicFunctionWithParallelContract.IncreaseValueWithInlineAndFailedPrePlugin):
                case nameof(BasicFunctionWithParallelContract.IncreaseValueWithFailedPrePluginAndPostPlugin):
                case nameof(BasicFunctionWithParallelContract.IncreaseValueWithInlineAndFailedPrePluginAndPostPlugin):
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
                        MethodName = nameof(BasicFunctionWithParallelContract.IncreaseValueFailed),
                        RefBlockNumber = transactionContext.BlockHeight - 1,
                        RefBlockPrefix =
                            BlockHelper.GetRefBlockPrefix(transactionContext.PreviousBlockHash)
                    });
                    break;
                }
            }
            
            return Task.FromResult(transactions.AsEnumerable());
        }
        
        public bool IsStopExecuting(ByteString txReturnValue)
        {
            return false;
        }
    }
}