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

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs8
{
    public class ResourceConsumptionPostExecutionPlugin : IPostExecutionPlugin, ISingletonDependency
    {
        private readonly IHostSmartContractBridgeContextService _contextService;
        private readonly ICalculateReadCostStrategy _readCostStrategy;
        private readonly ICalculateWriteCostStrategy _writeCostStrategy;
        private readonly ICalculateNetCostStrategy _netCostStrategy;
        private readonly ICalculateStoCostStrategy _stoCostStrategy;
        
        private const string AcsSymbol = "acs8";

        public ResourceConsumptionPostExecutionPlugin(IHostSmartContractBridgeContextService contextService,
            ICalculateReadCostStrategy readCostStrategy,
            ICalculateWriteCostStrategy writeCostStrategy,
            ICalculateStoCostStrategy stoCostStrategy,
            ICalculateNetCostStrategy netCostStrategy)
        {
            _contextService = contextService;
            _readCostStrategy = readCostStrategy;
            _writeCostStrategy = writeCostStrategy;
            _stoCostStrategy = stoCostStrategy;
            _netCostStrategy = netCostStrategy;
        }

        private static bool IsAcs8(IReadOnlyList<ServiceDescriptor> descriptors)
        {
            return descriptors.Any(service => service.File.GetIndentity() == AcsSymbol);
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

            var tokenStub = new TokenContractContainer.TokenContractStub
            {
                __factory = new TransactionGeneratingOnlyMethodStubFactory
                {
                    Sender = transactionContext.Transaction.To,
                    ContractAddress = tokenContractAddress
                }
            };
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

            // Transaction size related to NET Token.
            var netSize = transactionContext.Transaction.Size();
            // Transaction trace state set writes count related to STO Token.
            var writesCount = transactionContext.Trace.StateSet.Writes.Count;
            // Transaction trace state set reads count related to READ Token.
            var readsCount = transactionContext.Trace.StateSet.Reads.Count;
            var chainContext = new ChainContext
            {
                BlockHash = transactionContext.PreviousBlockHash,
                BlockHeight = transactionContext.BlockHeight - 1
            };
            var netCost = await _netCostStrategy.GetCostAsync(chainContext, netSize);
            var cpuCost = await _readCostStrategy.GetCostAsync(chainContext, readsCount);
            var stoCost = await _stoCostStrategy.GetCostAsync(chainContext, netSize);
            var ramCost = await _writeCostStrategy.GetCostAsync(chainContext, writesCount);
            var chargeResourceTokenTransaction = (await tokenStub.ChargeResourceToken.SendAsync(
                new ChargeResourceTokenInput
                {
                    NetCost = netCost,
                    StoCost = stoCost,
                    ReadCost = cpuCost,
                    WriteCost = ramCost,
                    Caller = transactionContext.Transaction.From
                })).Transaction;

            return new List<Transaction>
            {
                chargeResourceTokenTransaction
            };
        }
    }
}