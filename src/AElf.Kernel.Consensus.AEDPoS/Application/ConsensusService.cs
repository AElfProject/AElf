using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Kernel.Consensus.Infrastructure;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

[assembly: InternalsVisibleTo("AElf.Kernel.Consensus")]
namespace AElf.Kernel.Consensus.Application
{
    internal class ConsensusService : IConsensusService
    {
        private readonly IConsensusInformationGenerationService _consensusInformationGenerationService;
        private readonly ConsensusControlInformation _consensusControlInformation;
        private readonly IConsensusScheduler _consensusScheduler;
        private readonly IReaderFactory _readerFactory;
        private readonly ITriggerInformationProvider _triggerInformationProvider;
        private readonly IBlockTimeProvider _blockTimeProvider;

        public ILogger<ConsensusService> Logger { get; set; }

        private DateTime _nextMiningTime;

        public ConsensusService(IConsensusInformationGenerationService consensusInformationGenerationService,
            IConsensusScheduler consensusScheduler, ConsensusControlInformation consensusControlInformation,
            IReaderFactory readerFactory, ITriggerInformationProvider triggerInformationProvider,
            IBlockTimeProvider blockTimeProvider)
        {
            _consensusInformationGenerationService = consensusInformationGenerationService;
            _consensusControlInformation = consensusControlInformation;
            _readerFactory = readerFactory;
            _triggerInformationProvider = triggerInformationProvider;
            _blockTimeProvider = blockTimeProvider;
            _consensusScheduler = consensusScheduler;

            Logger = NullLogger<ConsensusService>.Instance;
        }

        public async Task TriggerConsensusAsync(ChainContext chainContext)
        {
            var now = DateTime.UtcNow;
            _blockTimeProvider.SetBlockTime(now);

            Logger.LogTrace($"Set block time to utc now: {now:hh:mm:ss.fff}. Trigger.");

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
            var now = DateTime.UtcNow;
            _blockTimeProvider.SetBlockTime(now);

            Logger.LogTrace($"Set block time to utc now: {now:hh:mm:ss.fff}. Validate Before.");

            var validationResult = await _readerFactory.Create(chainContext).ValidateConsensusBeforeExecution
                .CallAsync(_consensusInformationGenerationService.ParseHeaderExtraData(consensusExtraData));

            if (!validationResult.Success)
            {
                Logger.LogError($"Consensus validating before execution failed: {validationResult.Message}");
            }

            return validationResult.Success;
        }

        public async Task<bool> ValidateConsensusAfterExecutionAsync(ChainContext chainContext,
            byte[] consensusExtraData)
        {
            var now = DateTime.UtcNow;
            _blockTimeProvider.SetBlockTime(now);

            Logger.LogTrace($"Set block time to utc now: {now:hh:mm:ss.fff}. Validate After.");

            var validationResult = await _readerFactory.Create(chainContext).ValidateConsensusAfterExecution
                .CallAsync(_consensusInformationGenerationService.ParseHeaderExtraData(consensusExtraData));

            if (!validationResult.Success)
            {
                Logger.LogError($"Consensus validating after execution failed: {validationResult.Message}");
            }

            return validationResult.Success;
        }

        /// <inheritdoc />
        /// <summary>
        /// Get consensus block header extra data.
        /// </summary>
        /// <param name="chainContext"></param>
        /// <returns></returns>
        public async Task<byte[]> GetInformationToUpdateConsensusAsync(ChainContext chainContext)
        {
            _blockTimeProvider.SetBlockTime(_nextMiningTime);

            Logger.LogTrace($"Set block time to next mining time: {_nextMiningTime:hh:mm:ss.fff}. Extra Data.");

            return (await _readerFactory.Create(chainContext).GetInformationToUpdateConsensus
                .CallAsync(_triggerInformationProvider.GetTriggerInformationToGetExtraData())).ToByteArray();
        }

        public async Task<IEnumerable<Transaction>> GenerateConsensusTransactionsAsync(ChainContext chainContext)
        {
            _blockTimeProvider.SetBlockTime(_nextMiningTime);

            Logger.LogTrace($"Set block time to next mining time: {_nextMiningTime:hh:mm:ss.fff}. Txs.");

            var generatedTransactions =
                (await _readerFactory.Create(chainContext).GenerateConsensusTransactions
                    .CallAsync(_triggerInformationProvider.GetTriggerInformationToGenerateConsensusTransactions()))
                .Transactions
                .ToList();

            // Complete these transactions.
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