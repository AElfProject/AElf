using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.TestContract.BasicFunctionWithParallel;
using AElf.Contracts.TestKit;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Volo.Abp.DependencyInjection;

namespace AElf.Parallel.Tests
{
    public class DeleteDataFromStateDbPostExecutionPlugin : IPostExecutionPlugin, ISingletonDependency
    {
        private readonly IHostSmartContractBridgeContextService _contextService;

        public DeleteDataFromStateDbPostExecutionPlugin(IHostSmartContractBridgeContextService contextService)
        {
            _contextService = contextService;
        }
        
        public Task<IEnumerable<Transaction>> GetPostTransactionsAsync(IReadOnlyList<ServiceDescriptor> descriptors, ITransactionContext transactionContext)
        {
            if (!descriptors.Any(service => service.File.Name == "test_basic_function_with_parallel_contract.proto"))
            {
                return Task.FromResult(new List<Transaction>().AsEnumerable());
            }

            if (transactionContext.Transaction.To == ParallelTestHelper.BasicFunctionWithParallelContractAddress &&
                !transactionContext.Transaction.MethodName.EndsWith("WithPlugin"))
            {
                return Task.FromResult(new List<Transaction>().AsEnumerable());
            }

            var context = _contextService.Create();
            context.TransactionContext = transactionContext;

            var key = "TestKey";
            switch (transactionContext.Transaction.MethodName)
            {
                case nameof(BasicFunctionWithParallelContract.SetValueWithPlugin):
                {
                    var input = SetValueInput.Parser.ParseFrom(transactionContext.Transaction.Params);
                    key = input.Key;
                    break;
                }

                case nameof(BasicFunctionWithParallelContract.RemoveValueWithPlugin):
                {
                    var input = RemoveValueInput.Parser.ParseFrom(transactionContext.Transaction.Params);
                    key = input.Key;
                    break;
                }

                case nameof(BasicFunctionWithParallelContract.SetAfterRemoveValueWithPlugin):
                {
                    var input = SetAfterRemoveValueInput.Parser.ParseFrom(transactionContext.Transaction.Params);
                    key = input.Key;
                    break;
                }

                case nameof(BasicFunctionWithParallelContract.RemoveAfterSetValueWithPlugin):
                {
                    var input = RemoveAfterSetValueInput.Parser.ParseFrom(transactionContext.Transaction.Params);
                    key = input.Key;
                    break;
                }
            }

            var transaction = new Transaction
            {
                From = transactionContext.Transaction.From,
                To = ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                Params = new SetValueInput
                {
                    Key = $"{key}_post_plugin_key",
                    BoolValue = true,
                    Int64Value = 2,
                    StringValue = $"{key}_post_plugin_string",
                    MessageValue = new MessageValue
                    {
                        AddressValue = SampleAddress.AddressList[3],
                        BoolValue = true,
                        Int64Value = 2,
                        StringValue = $"{key}_post_plugin_message_string"
                    }
                }.ToByteString(),
                MethodName = nameof(BasicFunctionWithParallelContract.SetValue),
                RefBlockNumber = transactionContext.BlockHeight-1,
                RefBlockPrefix = ByteString.CopyFrom(transactionContext.PreviousBlockHash.Value.Take(4).ToArray())
            };
            
            var transactions = new List<Transaction>
            {
                transaction
            };
            
            transaction = new Transaction
            {
                From = transactionContext.Transaction.From,
                To = ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                Params = new RemoveValueInput
                {
                    Key = $"{key}_pre_plugin_key_for_delete"
                }.ToByteString(),
                MethodName = nameof(BasicFunctionWithParallelContract.RemoveValue),
                RefBlockNumber = transactionContext.BlockHeight-1,
                RefBlockPrefix = ByteString.CopyFrom(transactionContext.PreviousBlockHash.Value.Take(4).ToArray())
            };
            
            transactions.Add(transaction);
            
            return Task.FromResult(transactions.AsEnumerable());
        
        }
    }
}