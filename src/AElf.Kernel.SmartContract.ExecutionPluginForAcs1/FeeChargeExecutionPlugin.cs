using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs1;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Sdk;
using AElf.CSharp.Core;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf.Reflection;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs1
{
    public class FeeChargeExecutionPlugin : IExecutionPlugin, ISingletonDependency
    {
        private readonly IHostSmartContractBridgeContextService _contextService;

        public FeeChargeExecutionPlugin(IHostSmartContractBridgeContextService contextService)
        {
            _contextService = contextService;
        }
        private static bool IsAcs1(IReadOnlyList<ServiceDescriptor> descriptors)
        {
            return descriptors.Any(service => service.File.GetIndentity() == "acs1");
        }

        public async Task<IEnumerable<Transaction>> GetPreTransactionsAsync(IReadOnlyList<ServiceDescriptor> descriptors, ITransactionContext transactionContext)
        {
            if (!IsAcs1(descriptors))
            {
                return new List<Transaction>();
            }

            var context = _contextService.Create();
            context.TransactionContext = transactionContext;
            var selfStub = new FeeChargedContractContainer.FeeChargedContractStub()
            {
                __factory = new MethodStubFactory(context)
            };

            var fee = await selfStub.GetMethodFee.CallAsync(new MethodName
            {
                Name = context.TransactionContext.Transaction.MethodName
            });
            
            if (!fee.SymbolToAmount.Any())
            {
                return new List<Transaction>();
            }
            
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
                SymbolToAmount = {fee.SymbolToAmount}
            })).Transaction;
            return new List<Transaction>
            {
                chargeFeeTransaction
            };
        }
    }
}