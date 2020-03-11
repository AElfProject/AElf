using System.Collections.Generic;
using System.Threading.Tasks;
using Acs8;
using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf.Reflection;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee
{
    public class ResourceConsumptionPostExecutionPlugin : SmartContractExecutionPluginBase, IPostExecutionPlugin, ISingletonDependency
    {
        private readonly IHostSmartContractBridgeContextService _contextService;
        private readonly IResourceTokenFeeService _resourceTokenFeeService;

        public ResourceConsumptionPostExecutionPlugin(IHostSmartContractBridgeContextService contextService,
            IResourceTokenFeeService resourceTokenFeeService) : base("acs8")
        {
            _contextService = contextService;
            _resourceTokenFeeService = resourceTokenFeeService;
        }
        
        private static TokenContractContainer.TokenContractStub GetTokenContractStub(Address sender,
            Address contractAddress)
        {
            return new TokenContractContainer.TokenContractStub
            {
                __factory = new TransactionGeneratingOnlyMethodStubFactory
                {
                    Sender = sender,
                    ContractAddress = contractAddress
                }
            };
        }

        public async Task<IEnumerable<Transaction>> GetPostTransactionsAsync(
            IReadOnlyList<ServiceDescriptor> descriptors, ITransactionContext transactionContext)
        {
            if (!IsTargetAcsSymbol(descriptors))
            {
                return new List<Transaction>();
            }

            var context = _contextService.Create();
            context.TransactionContext = transactionContext;

            // Generate token contract stub.
            var tokenContractAddress = context.GetContractAddressByName(TokenSmartContractAddressNameProvider.Name);
            if (tokenContractAddress == null)
            {
                return new List<Transaction>();
            }

            var tokenStub = GetTokenContractStub(transactionContext.Transaction.To, tokenContractAddress);
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

            var chainContext = new ChainContext
            {
                BlockHash = transactionContext.PreviousBlockHash,
                BlockHeight = transactionContext.BlockHeight - 1
            };
            var chargeResourceTokenInput = new ChargeResourceTokenInput
            {
                Caller = transactionContext.Transaction.From
            };

            var feeCalculationResult =
                await _resourceTokenFeeService.CalculateTokenFeeAsync(transactionContext, chainContext);
            chargeResourceTokenInput.CostDic.Add(feeCalculationResult);

            var chargeResourceTokenTransaction =
                (await tokenStub.ChargeResourceToken.SendAsync(chargeResourceTokenInput)).Transaction;

            return new List<Transaction>
            {
                chargeResourceTokenTransaction
            };
        }
    }
}