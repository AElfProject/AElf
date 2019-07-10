using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs8;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForConstrainedTransaction
{
    public class ConstrainedTransactionExecutionPlugin : IPreExecutionPlugin, ISingletonDependency
    {
        private readonly IHostSmartContractBridgeContextService _contextService;

        public ConstrainedTransactionExecutionPlugin(IHostSmartContractBridgeContextService contextService)
        {
            _contextService = contextService;
        }

        private static bool IsConstrainedTransaction(IHostSmartContractBridgeContext context)
        {
            var consensusContractAddress = context.GetContractAddressByName(ConsensusSmartContractAddressNameProvider.Name);
            var transaction = context.TransactionContext.Transaction;
            if (transaction.To == consensusContractAddress)
            {
                var consensusConstrainedTransactions = new List<string>
                {
                    nameof(AEDPoSContractContainer.AEDPoSContractStub.InitialAElfConsensusContract),
                    nameof(AEDPoSContractContainer.AEDPoSContractStub.FirstRound),
                    nameof(AEDPoSContractContainer.AEDPoSContractStub.UpdateValue),
                    nameof(AEDPoSContractContainer.AEDPoSContractStub.NextRound),
                    nameof(AEDPoSContractContainer.AEDPoSContractStub.NextTerm),
                    nameof(AEDPoSContractContainer.AEDPoSContractStub.UpdateTinyBlockInformation)
                };
                if (consensusConstrainedTransactions.Contains(transaction.MethodName))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<IEnumerable<Transaction>> GetPreTransactionsAsync(
            IReadOnlyList<ServiceDescriptor> descriptors, ITransactionContext transactionContext)
        {
            var context = _contextService.Create();
            context.TransactionContext = transactionContext;

            if (!IsConstrainedTransaction(context))
            {
                return new List<Transaction>();
            }

            // Generate token contract stub.
            var electionContractAddress = context.GetContractAddressByName(ElectionSmartContractAddressNameProvider.Name);
            if (electionContractAddress == null)
            {
                return new List<Transaction>();
            }

            var electionContractStub = new ElectionContractContainer.ElectionContractStub
            {
                __factory = new TransactionGeneratingOnlyMethodStubFactory
                {
                    Sender = transactionContext.Transaction.From,
                    ContractAddress = electionContractAddress
                }
            };

            var announceElectionTransaction =
                (await electionContractStub.AnnounceElection.SendAsync(new Empty())).Transaction;

            return new List<Transaction>
            {
                announceElectionTransaction
            };
        }
    }
}