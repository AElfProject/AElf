using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs5;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs5
{
    public class MethodCallingThresholdPreExecutionPlugin : SmartContractExecutionPluginBase, IPreExecutionPlugin, ISingletonDependency
    {
        private readonly IHostSmartContractBridgeContextService _contextService;

        public MethodCallingThresholdPreExecutionPlugin(IHostSmartContractBridgeContextService contextService):base("acs5")
        {
            _contextService = contextService;
        }

        public async Task<IEnumerable<Transaction>> GetPreTransactionsAsync(
            IReadOnlyList<ServiceDescriptor> descriptors, ITransactionContext transactionContext)
        {
            if (!IsTargetAcsSymbol(descriptors))
            {
                return new List<Transaction>();
            }

            var context = _contextService.Create();
            context.TransactionContext = transactionContext;
            var selfStub = new ThresholdSettingContractContainer.ThresholdSettingContractStub
            {
                __factory = new MethodStubFactory(context)
            };

            var threshold = await selfStub.GetMethodCallingThreshold.CallAsync(new StringValue
            {
                Value = context.TransactionContext.Transaction.MethodName
            });

            // Generate token contract stub.
            var tokenContractAddress = context.GetContractAddressByName(TokenSmartContractAddressNameProvider.Name);
            if (tokenContractAddress == null)
            {
                return new List<Transaction>();
            }

            var tokenStub = new TokenContractContainer.TokenContractStub
            {
                __factory = new TransactionGeneratingOnlyMethodStubFactory
                {
                    Sender = transactionContext.Transaction.To,
                    ContractAddress = tokenContractAddress
                }
            };
            if (transactionContext.Transaction.To == tokenContractAddress &&
                transactionContext.Transaction.MethodName == nameof(tokenStub.CheckThreshold))
            {
                return new List<Transaction>();
            }

            var checkThresholdTransaction = (await tokenStub.CheckThreshold.SendAsync(new CheckThresholdInput
            {
                Sender = context.Sender,
                SymbolToThreshold = {threshold.SymbolToAmount},
                IsCheckAllowance = threshold.ThresholdCheckType == ThresholdCheckType.Allowance
            })).Transaction;

            return new List<Transaction>
            {
                checkThresholdTransaction
            };
        }
    }
}