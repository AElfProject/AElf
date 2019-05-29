using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs4;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Consensus.Application
{
    internal class ConsensusService : IConsensusService
    {
        private ConsensusCommand _consensusCommand;
        private readonly IConsensusScheduler _consensusScheduler;
        private readonly IConsensusReaderFactory _readerFactory;
        private readonly ITriggerInformationProvider _triggerInformationProvider;
        private readonly IBlockTimeProvider _blockTimeProvider;

        public ILogger<ConsensusService> Logger { get; set; }

        private Timestamp _nextMiningTime;

        public ConsensusService(IConsensusScheduler consensusScheduler,
            IConsensusReaderFactory readerFactory,
            ITriggerInformationProvider triggerInformationProvider,
            IBlockTimeProvider blockTimeProvider)
        {
            _readerFactory = readerFactory;
            _triggerInformationProvider = triggerInformationProvider;
            _blockTimeProvider = blockTimeProvider;
            _consensusScheduler = consensusScheduler;

            Logger = NullLogger<ConsensusService>.Instance;
        }

        public async Task TriggerConsensusAsync(ChainContext chainContext)
        {
            var now = DateTime.UtcNow.ToSafeDateTime();
            _blockTimeProvider.SetBlockTime(now);

            Logger.LogTrace($"Set block time to utc now: {now:hh:mm:ss.ffffff}. Trigger.");

            var triggerInformation = _triggerInformationProvider.GetTriggerInformationForConsensusCommand(new BytesValue());

            // Upload the consensus command.
            _consensusCommand = await _readerFactory.Create(chainContext)
                .GetConsensusCommand.CallAsync(triggerInformation);

            Logger.LogDebug($"Updated consensus command: {_consensusCommand}");

            // Update next mining time, also block time of both getting consensus extra data and txs.
            _nextMiningTime =
                DateTime.UtcNow.ToSafeDateTime().AddMilliseconds(_consensusCommand
                    .NextBlockMiningLeftMilliseconds).ToTimestamp();

            // Initial consensus scheduler.
            var blockMiningEventData = new ConsensusRequestMiningEventData(chainContext.BlockHash,
                chainContext.BlockHeight,
                _nextMiningTime,
                TimeSpan.FromMilliseconds(_consensusCommand
                    .LimitMillisecondsOfMiningBlock));
            _consensusScheduler.CancelCurrentEvent();
            _consensusScheduler.NewEvent(_consensusCommand.NextBlockMiningLeftMilliseconds,
                blockMiningEventData);

            Logger.LogTrace($"Set next mining time to: {_nextMiningTime:hh:mm:ss.ffffff}");
        }

        public async Task<bool> ValidateConsensusBeforeExecutionAsync(ChainContext chainContext,
            byte[] consensusExtraData)
        {
            var now = DateTime.UtcNow.ToSafeDateTime();
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
            var now = DateTime.UtcNow.ToSafeDateTime();
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
            _blockTimeProvider.SetBlockTime(_nextMiningTime.ToSafeDateTime());

            Logger.LogTrace($"Set block time to next mining time: {_nextMiningTime:hh:mm:ss.ffffff}. Extra Data.");

            return (await _readerFactory.Create(chainContext).GetInformationToUpdateConsensus
                    .CallAsync(await _triggerInformationProvider.GetTriggerInformationForBlockHeaderExtraDataAsync(
                        _consensusCommand.ToBytesValue()))).Value
                .ToByteArray();
        }

        public async Task<IEnumerable<Transaction>> GenerateConsensusTransactionsAsync(ChainContext chainContext)
        {
            _blockTimeProvider.SetBlockTime(_nextMiningTime.ToSafeDateTime());

            Logger.LogTrace($"Set block time to next mining time: {_nextMiningTime:hh:mm:ss.ffffff}. Txs.");

            var generatedTransactions =
                (await _readerFactory.Create(chainContext).GenerateConsensusTransactions
                    .CallAsync(await _triggerInformationProvider.GetTriggerInformationForConsensusTransactionsAsync(
                        _consensusCommand.ToBytesValue())))
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