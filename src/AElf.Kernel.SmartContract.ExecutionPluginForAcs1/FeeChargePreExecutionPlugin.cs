using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using Acs1;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs1
{
    public class FeeChargePreExecutionPlugin : IPreExecutionPlugin, ISingletonDependency
    {
        private readonly IHostSmartContractBridgeContextService _contextService;
        private readonly ISystemTransactionMethodNameListProvider _systemTransactionMethodNameListProvider;

        public FeeChargePreExecutionPlugin(IHostSmartContractBridgeContextService contextService,
            ISystemTransactionMethodNameListProvider systemTransactionMethodNameListProvider)
        {
            _contextService = contextService;
            _systemTransactionMethodNameListProvider = systemTransactionMethodNameListProvider;
        }

        private static bool IsAcs1(IReadOnlyList<ServiceDescriptor> descriptors)
        {
            return descriptors.Any(service => service.File.GetIndentity() == "acs1");
        }

        public async Task<IEnumerable<Transaction>> GetPreTransactionsAsync(
            IReadOnlyList<ServiceDescriptor> descriptors, ITransactionContext transactionContext)
        {
            var context = _contextService.Create();

            var systemContractAddresses = new List<Address>
            {
                context.GetZeroSmartContractAddress(context.ChainId),
                context.GetContractAddressByName(TokenSmartContractAddressNameProvider.Name),
                // TODO: Try to use contract address name providers - put providers to one place easy to ref.
                context.GetContractAddressByName(Hash.FromString("AElf.ContractNames.Consensus")),
                context.GetContractAddressByName(Hash.FromString("AElf.ContractNames.CrossChain"))
            };
            if (systemContractAddresses.Contains(transactionContext.Transaction.To))
            {
                if (_systemTransactionMethodNameListProvider.GetSystemTransactionMethodNameList()
                    .Contains(transactionContext.Transaction.MethodName))
                {
                    return new List<Transaction>();
                }
                
                var methodNameWhiteList = new List<string>
                {
                    nameof(TokenContractContainer.TokenContractStub.Create),
                    nameof(TokenContractContainer.TokenContractStub.Issue),
                    nameof(TokenContractContainer.TokenContractStub.CrossChainTransfer),
                    nameof(TokenContractContainer.TokenContractStub.CrossChainReceiveToken)
                };

                if (methodNameWhiteList.Contains(transactionContext.Transaction.MethodName))
                {
                    return new List<Transaction>();
                }
            }

            if (context.CurrentHeight < 2)
            {
                return new List<Transaction>();
            }

            var tokenContractAddress = context.GetContractAddressByName(TokenSmartContractAddressNameProvider.Name);

            if (!IsAcs1(descriptors) && transactionContext.Transaction.To != tokenContractAddress)
            {
                return new List<Transaction>();
            }

            context.TransactionContext = transactionContext;

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
                transactionContext.Transaction.MethodName == nameof(tokenStub.ChargeTransactionFees))
            {
                // Skip ChargeTransactionFees itself 
                return new List<Transaction>();
            }

            var chargeFeeTransaction = (await tokenStub.ChargeTransactionFees.SendAsync(new ChargeTransactionFeesInput
            {
                MethodName = transactionContext.Transaction.MethodName,
                ContractAddress = transactionContext.Transaction.To,
                TransactionSize = transactionContext.Transaction.Size()
            })).Transaction;
            return new List<Transaction>
            {
                chargeFeeTransaction
            };
        }
    }
}