using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs8;
using AElf.Contracts.MultiToken;
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
        private readonly ICalculateReadCostStrategy _readCostStrategy;
        private readonly ICalculateWriteCostStrategy _writeCostStrategy;
        private readonly ICalculateTrafficCostStrategy _trafficCostStrategy;
        private readonly ICalculateStorageCostStrategy _storageCostStrategy;
        
        private const string AcsSymbol = "acs8";

        public ResourceConsumptionPostExecutionPlugin(IHostSmartContractBridgeContextService contextService,
            //TODO: change strategy implement
            ICalculateReadCostStrategy readCostStrategy,
            ICalculateWriteCostStrategy writeCostStrategy,
            ICalculateStorageCostStrategy storageCostStrategy,
            ICalculateTrafficCostStrategy trafficCostStrategy)
        {
            _contextService = contextService;
            _readCostStrategy = readCostStrategy;
            _writeCostStrategy = writeCostStrategy;
            _storageCostStrategy = storageCostStrategy;
            _trafficCostStrategy = trafficCostStrategy;
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

            // Transaction size related to TRAFFIC Token.
            var trafficSize = transactionContext.Transaction.Size();
            // Transaction trace state set writes count related to STORAGE Token.
            var writesCount = transactionContext.Trace.StateSet.Writes.Count;
            // Transaction trace state set reads count related to WRITE Token.
            var readsCount = transactionContext.Trace.StateSet.Reads.Count;
            var chainContext = new ChainContext
            {
                BlockHash = transactionContext.PreviousBlockHash,
                BlockHeight = transactionContext.BlockHeight - 1
            };
            var trafficCost = await _trafficCostStrategy.GetCostAsync(chainContext, trafficSize);
            var readCost = await _readCostStrategy.GetCostAsync(chainContext, readsCount);
            var storageCost = await _storageCostStrategy.GetCostAsync(chainContext, trafficSize);
            var writeCost = await _writeCostStrategy.GetCostAsync(chainContext, writesCount);
            var chargeResourceTokenTransaction = (await tokenStub.ChargeResourceToken.SendAsync(
                new ChargeResourceTokenInput
                {
                    TrafficCost = trafficCost,
                    StorageCost = storageCost,
                    ReadCost = readCost,
                    WriteCost = writeCost,
                    Caller = transactionContext.Transaction.From
                })).Transaction;

            return new List<Transaction>
            {
                chargeResourceTokenTransaction
            };
        }
    }
}