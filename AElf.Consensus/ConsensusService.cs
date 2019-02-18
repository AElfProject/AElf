using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Account;
using AElf.Kernel.Services;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AElf.Consensus
{
    public class ConsensusService : IConsensusService
    {
        private readonly IConsensusObserver _consensusObserver;
        private readonly IExecutingService _executingService;
        private readonly IConsensusInformationGenerationService _consensusInformationGenerationService;
        private readonly IAccountService _accountService;

        private IDisposable _consensusObservables;

        private byte[] _latestGeneratedConsensusInformation;

        public ILogger<ConsensusService> Logger { get; set; }

        public ConsensusService(IConsensusObserver consensusObserver, IExecutingService executingService,
            IConsensusInformationGenerationService consensusInformationGenerationService,
            IAccountService accountService)
        {
            _consensusObserver = consensusObserver;
            _executingService = executingService;
            _consensusInformationGenerationService = consensusInformationGenerationService;
            _accountService = accountService;

            Logger = NullLogger<ConsensusService>.Instance;
        }

        public async Task TriggerConsensus(int chainId)
        {
            var consensusCommand = ExecuteConsensusContract(chainId, await _accountService.GetAccountAsync(),
                ConsensusMethod.GetConsensusCommand, Timestamp.FromDateTime(DateTime.UtcNow)).ToByteArray();

            // Initial or update the schedule.
            _consensusObservables?.Dispose();
            _consensusObservables = _consensusObserver.Subscribe(consensusCommand);

            _consensusInformationGenerationService.Tell(consensusCommand);
        }

        public Task StopConsensus()
        {
            _consensusObservables?.Dispose();
            return Task.CompletedTask;
        }

        public async Task<bool> ValidateConsensus(int chainId, byte[] consensusInformation)
        {
            return ExecuteConsensusContract(chainId, await _accountService.GetAccountAsync(),
                    ConsensusMethod.ValidateConsensus, consensusInformation)
                .DeserializeToPbMessage<ValidationResult>().Success;
        }

        public async Task<byte[]> GetNewConsensusInformation(int chainId)
        {
            var newConsensusInformation = ExecuteConsensusContract(chainId, await _accountService.GetAccountAsync(),
                ConsensusMethod.GetNewConsensusInformation,
                _consensusInformationGenerationService.GenerateExtraInformationAsync()).DeserializeToBytes();

            _latestGeneratedConsensusInformation = newConsensusInformation;

            return newConsensusInformation;
        }

        public async Task<IEnumerable<Transaction>> GenerateConsensusTransactions(int chainId, ulong refBlockHeight,
            byte[] refBlockPrefix)
        {
            var generatedTransactions = ExecuteConsensusContract(chainId, await _accountService.GetAccountAsync(),
                    ConsensusMethod.GenerateConsensusTransactions, refBlockHeight, refBlockPrefix,
                    _consensusInformationGenerationService.GenerateExtraInformationForTransactionAsync(
                        _latestGeneratedConsensusInformation, chainId)).DeserializeToPbMessage<TransactionList>()
                .Transactions
                .ToList();

            return generatedTransactions;
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
            return traces.Last().RetVal?.Data;
        }

        private enum ConsensusMethod
        {
            ValidateConsensus,
            GetNewConsensusInformation,
            GenerateConsensusTransactions,
            GetConsensusCommand
        }
    }
}