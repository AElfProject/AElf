using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Execution.Execution;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Kernel.Types;
using AElf.SmartContract;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace AElf.Consensus
{
    public class ConsensusService : IConsensusService
    {
        private readonly IConsensusObserver _consensusObserver;
        private readonly IExecutingService _executingService;
        private readonly IConsensusInformationGenerationService _consensusInformationGenerationService;
        private readonly StateManager _stateManager;

        public IEventBus EventBus { get; set; }

        private IDisposable _consensusObservables = null;

        private byte[] _latestGeneratedConsensusInformation;

        public ConsensusService(IConsensusObserver consensusObserver, IExecutingService executingService,
            IConsensusInformationGenerationService consensusInformationGenerationService, StateManager stateManager)
        {
            _consensusObserver = consensusObserver;
            _executingService = executingService;
            _consensusInformationGenerationService = consensusInformationGenerationService;
            _stateManager = stateManager;

            EventBus = NullLocalEventBus.Instance;
        }

        public bool ValidateConsensus(int chainId, Address fromAddress, byte[] consensusInformation)
        {
            return ExecuteConsensusContract(chainId, fromAddress, ConsensusMethod.ValidateConsensus, consensusInformation)
                .DeserializeToPbMessage<ValidationResult>().Success;
        }

        public byte[] GetNewConsensusInformation(int chainId, Address fromAddress)
        {
            var newConsensusInformation =
                ExecuteConsensusContract(chainId, fromAddress, ConsensusMethod.GetNewConsensusInformation,
                        _consensusInformationGenerationService.GenerateExtraInformationAsync())
                    .DeserializeToBytes();
            _latestGeneratedConsensusInformation = newConsensusInformation;
            return newConsensusInformation;
        }

        public IEnumerable<Transaction> GenerateConsensusTransactions(int chainId, Address fromAddress,
            ulong refBlockHeight,
            byte[] refBlockPrefix)
        {
            return ExecuteConsensusContract(chainId, fromAddress, ConsensusMethod.GenerateConsensusTransactions,
                    refBlockHeight, refBlockPrefix,
                    _consensusInformationGenerationService.GenerateExtraInformationForTransactionAsync(
                        _latestGeneratedConsensusInformation)).DeserializeToPbMessage<TransactionList>().Transactions
                .ToList();
        }

        public byte[] GetConsensusCommand(int chainId, Address fromAddress)
        {
            return ExecuteConsensusContract(chainId, fromAddress, ConsensusMethod.GetConsensusCommand,
                Timestamp.FromDateTime(DateTime.UtcNow)).ToByteArray();
        }

        private ByteString ExecuteConsensusContract(int chainId, Address fromAddress, ConsensusMethod consensusMethod,
            params object[] objects)
        {
            var tx = new Transaction
            {
                From = fromAddress,
                To = ContractHelpers.GetConsensusContractAddress(chainId),
                MethodName = consensusMethod.ToString(),
                Params = ByteString.CopyFrom(ParamsPacker.Pack(objects))
            };

            var traces = _executingService.ExecuteAsync(new List<Transaction> {tx},
                chainId, DateTime.UtcNow, new CancellationToken(), null,
                TransactionType.ContractTransaction, true).Result;
            CommitChangesAsync(traces.Last()).Wait();
            return traces.Last().RetVal?.Data;
        }

        private async Task CommitChangesAsync(TransactionTrace trace)
        {
            await trace.SmartCommitChangesAsync(_stateManager);
        }

        enum ConsensusMethod
        {
            ValidateConsensus,
            GetNewConsensusInformation,
            GenerateConsensusTransactions,
            GetConsensusCommand
        }
    }
}