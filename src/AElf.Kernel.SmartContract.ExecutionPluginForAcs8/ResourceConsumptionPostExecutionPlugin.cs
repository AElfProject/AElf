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
        private const string AcsSymbol = "acs8";

        public ResourceConsumptionPostExecutionPlugin(IHostSmartContractBridgeContextService contextService)
        {
            _contextService = contextService;
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
            var transactionSize = transactionContext.Transaction.Size();
            // Transaction trace state set writes count related to STO Token.
            var writesCount = transactionContext.Trace.StateSet.Writes.Count;
            // Transaction trace state set reads count related to CPU Token.
            var readsCount = transactionContext.Trace.StateSet.Reads.Count;

            var chargeResourceTokenTransaction = (await tokenStub.ChargeResourceToken.SendAsync(
                new ChargeResourceTokenInput
                {
                    TransactionSize = transactionSize,
                    WritesCount = writesCount,
                    ReadsCount = readsCount,
                    Caller = transactionContext.Transaction.From
                })).Transaction;

            return new List<Transaction>
            {
                chargeResourceTokenTransaction
            };
        }
    }
}