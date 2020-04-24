using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs8;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee
{
    internal class ResourceConsumptionPreExecutionPlugin : SmartContractExecutionPluginBase, IPreExecutionPlugin,
        ISingletonDependency
    {
        private readonly IHostSmartContractBridgeContextService _contextService;
        private readonly IContractReaderFactory<TokenContractImplContainer.TokenContractImplStub>
            _contractReaderFactory;

        public ResourceConsumptionPreExecutionPlugin(IHostSmartContractBridgeContextService contextService,
            IContractReaderFactory<TokenContractImplContainer.TokenContractImplStub> contractReaderFactory) :
            base("acs8")
        {
            _contextService = contextService;
            _contractReaderFactory = contractReaderFactory;
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

            // Generate token contract stub.
            var tokenContractAddress = context.GetContractAddressByName(TokenSmartContractAddressNameProvider.StringName);
            if (tokenContractAddress == null)
            {
                return new List<Transaction>();
            }

            var tokenStub = _contractReaderFactory.Create(new ContractReaderContext
            {
                ContractAddress = tokenContractAddress,
                Sender = transactionContext.Transaction.To
            });
            
            if (transactionContext.Transaction.To == tokenContractAddress &&
                transactionContext.Transaction.MethodName == nameof(tokenStub.ChargeResourceToken))
            {
                return new List<Transaction>();
            }

            if (transactionContext.Transaction.To == context.Self &&
                transactionContext.Transaction.MethodName == nameof(ResourceConsumptionContractContainer
                    .ResourceConsumptionContractStub.BuyResourceToken))
            {
                return new List<Transaction>();
            }

            var checkResourceTokenTransaction = tokenStub.CheckResourceToken.GetTransaction(new Empty());

            return new List<Transaction>
            {
                checkResourceTokenTransaction
            };
        }

        public bool IsStopExecuting(ByteString txReturnValue)
        {
            return false;
        }
    }
}