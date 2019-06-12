using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs5;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs6;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf.Reflection;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs5
{
    public class ProfitSharingExecutionPlugin : IExecutionPlugin, ISingletonDependency
    {
        private readonly IHostSmartContractBridgeContextService _contextService;

        public ProfitSharingExecutionPlugin(IHostSmartContractBridgeContextService contextService)
        {
            _contextService = contextService;
        }
        private static bool IsAcs5(IReadOnlyList<ServiceDescriptor> descriptors)
        {
            return descriptors.Any(service => service.File.GetIndentity() == "acs5");
        }

        public async Task<IEnumerable<Transaction>> GetPreTransactionsAsync(IReadOnlyList<ServiceDescriptor> descriptors, ITransactionContext transactionContext)
        {
            if (!IsAcs5(descriptors))
            {
                return new List<Transaction>();
            }

            var context = _contextService.Create();
            context.TransactionContext = transactionContext;
            var selfStub = new ProfitSharingContractContainer.ProfitSharingContractStub
            {
                __factory = new MethodStubFactory(context)
            };

            var profit = await selfStub.GetProfitReceivers.CallAsync(new MethodName()
            {
                Name = context.TransactionContext.Transaction.MethodName
            });
            var tokenContractAddress = context.GetContractAddressByName(TokenSmartContractAddressNameProvider.Name);
            var tokenStub = new TokenContractContainer.TokenContractStub()
            {
                __factory = new TransactionGeneratingOnlyMethodStubFactory()
                {
                    Sender = transactionContext.Transaction.From,
                    ContractAddress = tokenContractAddress
                }
            };
            if (transactionContext.Transaction.To == tokenContractAddress &&
                transactionContext.Transaction.MethodName == nameof(tokenStub.ChargeTransactionFees))
            {
                // Skip ChargeTransactionFees itself 
                return new List<Transaction>();
            }
            
            var chargeFeeTransaction = (await tokenStub.ChargeTransactionFees.SendAsync(new ChargeTransactionFeesInput
            {
                Amount = profit.BaseAmount,
                Symbol = profit.BaseSymbol
            })).Transaction;
            return new List<Transaction>()
            {
                chargeFeeTransaction
            };
        }
    }
}