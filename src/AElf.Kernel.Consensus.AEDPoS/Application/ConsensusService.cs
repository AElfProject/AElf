using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Kernel.Consensus.Infrastructure;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Consensus.Application
{
    internal class ConsensusService : IConsensusService
    {

        private readonly IConsensusInformationGenerationService _consensusInformationGenerationService;
        private readonly ConsensusControlInformation _consensusControlInformation;
        private readonly IConsensusScheduler _consensusScheduler;
        private readonly IReaderFactory _readerFactory;
        private readonly ITriggerInformationProvider _triggerInformationProvider;
        public ILogger<ConsensusService> Logger { get; set; }

        private DateTime _nextMiningTime;

        public ConsensusService(IConsensusInformationGenerationService consensusInformationGenerationService,
            IConsensusScheduler consensusScheduler, ConsensusControlInformation consensusControlInformation,
            IReaderFactory readerFactory, ITriggerInformationProvider triggerInformationProvider)
        {
            _consensusInformationGenerationService = consensusInformationGenerationService;
            _consensusControlInformation = consensusControlInformation;
            _readerFactory = readerFactory;
            _triggerInformationProvider = triggerInformationProvider;
            _consensusScheduler = consensusScheduler;

            Logger = NullLogger<ConsensusService>.Instance;
        }

        public async Task TriggerConsensusAsync(ChainContext chainContext)
        {
            var triggerInformation = _triggerInformationProvider.GetTriggerInformationToGetConsensusCommand();

            // Upload the consensus command.
            _consensusControlInformation.ConsensusCommand = await _readerFactory.Create(chainContext)
                .GetConsensusCommand.CallAsync(triggerInformation);

            Logger.LogDebug($"Updated consensus command: {_consensusControlInformation.ConsensusCommand}");

            // Initial consensus scheduler.
            var blockMiningEventData = new ConsensusRequestMiningEventData(chainContext.BlockHash,
                chainContext.BlockHeight,
                _consensusControlInformation.ConsensusCommand.ExpectedMiningTime.ToDateTime(),
                TimeSpan.FromMilliseconds(_consensusControlInformation.ConsensusCommand
                    .LimitMillisecondsOfMiningBlock));
            _consensusScheduler.CancelCurrentEvent();
            _consensusScheduler.NewEvent(_consensusControlInformation.ConsensusCommand.NextBlockMiningLeftMilliseconds,
                blockMiningEventData);

            // Update next mining time, also block time of both getting consensus extra data and txs.
            _nextMiningTime =
                DateTime.UtcNow.AddMilliseconds(_consensusControlInformation.ConsensusCommand
                    .NextBlockMiningLeftMilliseconds);
        }

        public async Task<bool> ValidateConsensusBeforeExecutionAsync(ChainContext chainContext,
            byte[] consensusExtraData)
        {
            var validationResult = await _consensusInformationGenerationService.ExecuteContractAsync<ValidationResult>(
                chainContext, ConsensusConsts.ValidateConsensusBeforeExecution,
                ,
                DateTime.UtcNow);
            
            validationResult = await _readerFactory.Create(chainContext).ValidateConsensusBeforeExecution.CallAsync(_consensusInformationGenerationService.ParseConsensusTriggerInformation(consensusExtraData))

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
                DateTime.UtcNow);

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
            return await _consensusInformationGenerationService.GetInformationToUpdateConsensusAsync(chainContext,
                _nextMiningTime);
        }

        public async Task<IEnumerable<Transaction>> GenerateConsensusTransactionsAsync(ChainContext chainContext)
        {
            var generatedTransactions =
                (await _consensusInformationGenerationService.ExecuteContractAsync<TransactionList>(chainContext,
                    ConsensusConsts.GenerateConsensusTransactions,
                    _consensusInformationGenerationService.GetTriggerInformation(TriggerType.ConsensusTransactions), _nextMiningTime))
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