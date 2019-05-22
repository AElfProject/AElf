using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Consensus.Infrastructure;
using AElf.Types;
using AElf.Kernel.Consensus.AEDPoS.Application;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Consensus.Application
{
    // TODO: Use xxprovider to refactor.
    public class ConsensusService : IConsensusService
    {
        private readonly IConsensusInformationGenerationService _consensusInformationGenerationService;
        private readonly ConsensusControlInformation _consensusControlInformation;
        private readonly IConsensusScheduler _consensusScheduler;
        public ILogger<ConsensusService> Logger { get; set; }

        private Timestamp _nextMiningTime;

        // Workaround
        public ConsensusService(IConsensusInformationGenerationService consensusInformationGenerationService,
            IConsensusScheduler consensusScheduler, IServiceProvider serviceProvider) : this(
            consensusInformationGenerationService,
            consensusScheduler,
            serviceProvider.GetRequiredService<ConsensusControlInformation>()
        )
        {
        }

        internal ConsensusService(IConsensusInformationGenerationService consensusInformationGenerationService,
            IConsensusScheduler consensusScheduler, ConsensusControlInformation consensusControlInformation)
        {
            _consensusInformationGenerationService = consensusInformationGenerationService;
            _consensusControlInformation = consensusControlInformation;
            _consensusScheduler = consensusScheduler;

            Logger = NullLogger<ConsensusService>.Instance;
        }

        public async Task TriggerConsensusAsync(ChainContext chainContext)
        {
            var triggerInformation =
                _consensusInformationGenerationService.GetTriggerInformation(TriggerType.ConsensusCommand);
            // Upload the consensus command.
            _consensusControlInformation.ConsensusCommand =
                await _consensusInformationGenerationService.ExecuteContractAsync<ConsensusCommand>(chainContext,
                    ConsensusConsts.GetConsensusCommand, triggerInformation, DateTime.UtcNow.ToTimestamp());

            Logger.LogDebug($"Updated consensus command: {_consensusControlInformation.ConsensusCommand}");

            // Initial consensus scheduler.
            var blockMiningEventData = new ConsensusRequestMiningEventData(chainContext.BlockHash,
                chainContext.BlockHeight,
                _consensusControlInformation.ConsensusCommand.ExpectedMiningTime,
                TimeSpan.FromTicks(TimeSpan.TicksPerMillisecond.Mul(_consensusControlInformation.ConsensusCommand
                    .LimitMillisecondsOfMiningBlock)));
            _consensusScheduler.CancelCurrentEvent();
            _consensusScheduler.NewEvent(_consensusControlInformation.ConsensusCommand.NextBlockMiningLeftMilliseconds,
                blockMiningEventData);

            // Update next mining time, also block time of both getting consensus extra data and txs.
            _nextMiningTime =
                DateTime.UtcNow.ToSafeDateTime().AddMilliseconds(_consensusControlInformation.ConsensusCommand
                    .NextBlockMiningLeftMilliseconds).ToTimestamp();
        }

        public async Task<bool> ValidateConsensusBeforeExecutionAsync(ChainContext chainContext,
            byte[] consensusExtraData)
        {
            var validationResult = await _consensusInformationGenerationService.ExecuteContractAsync<ValidationResult>(
                chainContext, ConsensusConsts.ValidateConsensusBeforeExecution,
                _consensusInformationGenerationService.ParseConsensusTriggerInformation(consensusExtraData),
                DateTime.UtcNow.ToTimestamp());

            if (!validationResult.Success)
            {
                Logger.LogError($"Consensus validating before execution failed: {validationResult.Message}");
            }

            return validationResult.Success;
        }

        public async Task<bool> ValidateConsensusAfterExecutionAsync(ChainContext chainContext,
            byte[] consensusExtraData)
        {
            var validationResult = await _consensusInformationGenerationService.ExecuteContractAsync<ValidationResult>(
                chainContext, ConsensusConsts.ValidateConsensusAfterExecution,
                _consensusInformationGenerationService.ParseConsensusTriggerInformation(consensusExtraData),
                DateTime.UtcNow.ToTimestamp());

            if (!validationResult.Success)
            {
                Logger.LogError($"Consensus validating after execution failed: {validationResult.Message}");
            }

            return validationResult.Success;
        }

        /// <summary>
        /// Get consensus block header extra data.
        /// </summary>
        /// <param name="chainContext"></param>
        /// <returns></returns>
        public async Task<byte[]> GetInformationToUpdateConsensusAsync(ChainContext chainContext)
        {
            return await _consensusInformationGenerationService.GetInformationToUpdateConsensusAsync(chainContext, _nextMiningTime);
        }

        public async Task<IEnumerable<Transaction>> GenerateConsensusTransactionsAsync(ChainContext chainContext)
        {
            var generatedTransactions =
                (await _consensusInformationGenerationService.ExecuteContractAsync<TransactionList>(chainContext,
                    ConsensusConsts.GenerateConsensusTransactions,
                    _consensusInformationGenerationService.GetTriggerInformation(TriggerType.ConsensusTransactions),
                    _nextMiningTime))
                .Transactions
                .ToList();

            // Supply these transactions.
            foreach (var generatedTransaction in generatedTransactions)
            {
                generatedTransaction.RefBlockNumber = chainContext.BlockHeight;
                generatedTransaction.RefBlockPrefix =
                    ByteString.CopyFrom(chainContext.BlockHash.Value.Take(4).ToArray());
            }

            return generatedTransactions;
        }
    }
}