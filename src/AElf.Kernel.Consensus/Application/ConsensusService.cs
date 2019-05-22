using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Consensus.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Consensus.Application
{
    internal class ConsensusService : IConsensusService
    {
        private readonly ConsensusControlInformation _consensusControlInformation;
        private readonly IConsensusScheduler _consensusScheduler;
        private readonly IConsensusReaderFactory _readerFactory;
        private readonly ITriggerInformationProvider _triggerInformationProvider;
        private readonly IBlockTimeProvider _blockTimeProvider;

        public ILogger<ConsensusService> Logger { get; set; }

        private DateTime _nextMiningTime;

        public ConsensusService(IConsensusScheduler consensusScheduler,
            ConsensusControlInformation consensusControlInformation, IConsensusReaderFactory readerFactory,
            ITriggerInformationProvider triggerInformationProvider,
            IBlockTimeProvider blockTimeProvider)
        {
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

            Logger.LogTrace($"Set block time to utc now: {now:hh:mm:ss.ffffff}. Trigger.");

            var triggerInformation = _triggerInformationProvider.GetTriggerInformationForConsensusCommand();

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

            Logger.LogTrace($"Set next mining time to: {_nextMiningTime:hh:mm:ss.ffffff}");
        }

        public async Task<bool> ValidateConsensusBeforeExecutionAsync(ChainContext chainContext,
            byte[] consensusExtraData)
        {
            var now = DateTime.UtcNow;
            _blockTimeProvider.SetBlockTime(now);

            Logger.LogTrace($"Set block time to utc now: {now:hh:mm:ss.ffffff}. Validate Before.");

            var validationResult = await _readerFactory.Create(chainContext).ValidateConsensusBeforeExecution
                .CallAsync(new BytesValue {Value = ByteString.CopyFrom(consensusExtraData)});

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

            Logger.LogTrace($"Set block time to utc now: {now:hh:mm:ss.ffffff}. Validate After.");

            var validationResult = await _readerFactory.Create(chainContext).ValidateConsensusAfterExecution
                .CallAsync(new BytesValue {Value = ByteString.CopyFrom(consensusExtraData)});

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

            Logger.LogTrace($"Set block time to next mining time: {_nextMiningTime:hh:mm:ss.ffffff}. Extra Data.");

            return (await _readerFactory.Create(chainContext).GetInformationToUpdateConsensus
                    .CallAsync(_triggerInformationProvider.GetTriggerInformationForBlockHeaderExtraData())).Value
                .ToByteArray();
        }

        public async Task<IEnumerable<Transaction>> GenerateConsensusTransactionsAsync(ChainContext chainContext)
        {
            _blockTimeProvider.SetBlockTime(_nextMiningTime);

            Logger.LogTrace($"Set block time to next mining time: {_nextMiningTime:hh:mm:ss.ffffff}. Txs.");

            var generatedTransactions =
                (await _readerFactory.Create(chainContext).GenerateConsensusTransactions
                    .CallAsync(_triggerInformationProvider.GetTriggerInformationForConsensusTransactions()))
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