using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs5;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs6;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;
using CreateProfitItemInput = AElf.Contracts.Profit.CreateProfitItemInput;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs5
{
    public class ProfitSharingExecutionPlugin : IExecutionPlugin, ISingletonDependency
    {
        private readonly IHostSmartContractBridgeContextService _contextService;
        private const string AcsSymbol = "acs5";

        public ProfitSharingExecutionPlugin(IHostSmartContractBridgeContextService contextService)
        {
            _contextService = contextService;
        }

        private static bool IsAcs5(IReadOnlyList<ServiceDescriptor> descriptors)
        {
            return descriptors.Any(service => service.File.GetIndentity() == AcsSymbol);
        }

        public async Task<IEnumerable<Transaction>> GetPreTransactionsAsync(
            IReadOnlyList<ServiceDescriptor> descriptors, ITransactionContext transactionContext)
        {
            return new List<Transaction>();
/*

            if (!IsAcs5(descriptors))
            {
                return new List<Transaction>();
            }
            //descriptors.ToList().ForEach(service => service.Methods.ToList().ForEach(method => method.CustomOptions.TryGetBool(506001, out var isView)));

            var context = _contextService.Create();
            context.TransactionContext = transactionContext;
            var selfStub = new ProfitSharingContractContainer.ProfitSharingContractStub
            {
                __factory = new MethodStubFactory(context)
            };

            var profitFee = await selfStub.GetMethodProfitFee.CallAsync(new StringValue
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
                    Sender = transactionContext.Transaction.From,
                    ContractAddress = tokenContractAddress
                }
            };
            if (transactionContext.Transaction.To == tokenContractAddress &&
                transactionContext.Transaction.MethodName == nameof(tokenStub.ChargeMethodProfits))
            {
                // Skip ChargeMethodProfits itself 
                return new List<Transaction>();
            }

            // Generate profit contract stub.
            var profitContractAddress = context.GetContractAddressByName(Hash.FromString("AElf.ContractNames.Profit"));
            if (profitContractAddress == null)
            {
                return new List<Transaction>();
            }

            var profitStub = new ProfitContractContainer.ProfitContractStub
            {
                __factory = new TransactionGeneratingOnlyMethodStubFactory
                {
                    Sender = transactionContext.Transaction.To,
                    ContractAddress = profitContractAddress
                }
            };

            // Create a contract profit item for this contract.
            await profitStub.CreateTreasuryProfitItem.SendAsync(new CreateProfitItemInput
            {
                IsReleaseAllBalanceEveryTimeByDefault = true
            });

            var chargeProfitTransaction = (await tokenStub.ChargeMethodProfits.SendAsync(new ChargeMethodProfitsInput
            {
                SymbolToAmount = {profitFee.SymbolToAmount}
            })).Transaction;
            return new List<Transaction>
            {
                chargeProfitTransaction
            };*/
        }
    }
}