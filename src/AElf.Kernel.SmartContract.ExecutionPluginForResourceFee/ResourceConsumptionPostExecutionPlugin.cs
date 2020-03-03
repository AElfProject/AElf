using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs8;
using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf.Reflection;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee
{
    public class ResourceConsumptionPostExecutionPlugin : IPostExecutionPlugin, ISingletonDependency
    {
        private readonly IHostSmartContractBridgeContextService _contextService;
        private const string AcsSymbol = "acs8";
        private readonly List<IResourceTokenFeeProvider> _resourceTokenFeeProviders;

        public ResourceConsumptionPostExecutionPlugin(IHostSmartContractBridgeContextService contextService,
            IServiceContainer<IResourceTokenFeeProvider> resourceTokenFeeProviders)
        {
            _contextService = contextService;
            _resourceTokenFeeProviders = resourceTokenFeeProviders.ToLookup(p => p.GetType())
                .Select(coll => coll.First()).ToList();
        }

        private static bool IsAcs8(IReadOnlyList<ServiceDescriptor> descriptors)
        {
            return descriptors.Any(service => service.File.GetIdentity() == AcsSymbol);
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
            if (!IsAcs8(descriptors))
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
            foreach (var tokenProvider in _resourceTokenFeeProviders)
            {
                chargeResourceTokenInput.CostDic[tokenProvider.TokenName] =
                    await tokenProvider.CalculateTokenFeeAsync(transactionContext, chainContext);
            }

            var chargeResourceTokenTransaction =
                (await tokenStub.ChargeResourceToken.SendAsync(chargeResourceTokenInput)).Transaction;

            return new List<Transaction>
            {
                chargeResourceTokenTransaction
            };
        }
    }
}