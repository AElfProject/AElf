using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Infrastructure;
using AElf.Kernel.EventMessages;
using AElf.Kernel.SmartContract.Application;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Consensus.Application
{
    public class ConsensusService : IConsensusService
    {
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;

        private readonly IConsensusInformationGenerationService _consensusInformationGenerationService;
        private readonly IBlockchainService _blockchainService;
        private readonly ConsensusControlInformation _consensusControlInformation;
        private readonly IConsensusScheduler _consensusScheduler;
        private readonly ISmartContractAddressService _smartContractAddressService;
        public ILogger<ConsensusService> Logger { get; set; }

        private DateTime _nextMiningTime;

        public ConsensusService(IConsensusInformationGenerationService consensusInformationGenerationService,
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            IConsensusScheduler consensusScheduler, IBlockchainService blockchainService,
            ConsensusControlInformation consensusControlInformation,
            ISmartContractAddressService smartContractAddressService)
        {
            _consensusInformationGenerationService = consensusInformationGenerationService;
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _blockchainService = blockchainService;
            _consensusControlInformation = consensusControlInformation;
            _smartContractAddressService = smartContractAddressService;
            _consensusScheduler = consensusScheduler;

            Logger = NullLogger<ConsensusService>.Instance;
        }

        public async Task TriggerConsensusAsync(ChainContext chainContext)
        {
            var triggerInformation = _consensusInformationGenerationService.GetTriggerInformation();
            // Upload the consensus command.
            var commandBytes = await ExecuteContractAsync(chainContext, ConsensusConsts.GetConsensusCommand,
                triggerInformation, DateTime.UtcNow);
            _consensusControlInformation.ConsensusCommand =
                ConsensusCommand.Parser.ParseFrom(commandBytes.ToByteArray());

            // Initial consensus scheduler.
            var blockMiningEventData = new ConsensusRequestMiningEventData(chainContext.BlockHash, chainContext.BlockHeight,
                _consensusControlInformation.ConsensusCommand.LimitMillisecondsOfMiningBlock);
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
            var validationResult = ValidationResult.Parser.ParseFrom(
                await ExecuteContractAsync(chainContext, ConsensusConsts.ValidateConsensusBeforeExecution,
                    _consensusInformationGenerationService.ParseConsensusTriggerInformation(consensusExtraData), DateTime.UtcNow));

            if (!validationResult.Success)
            {
                Logger.LogError($"Consensus validating before execution failed: {validationResult.Message}");
            }

            return validationResult.Success;
        }

        public async Task<bool> ValidateConsensusAfterExecutionAsync(ChainContext chainContext,
            byte[] consensusExtraData)
        {
            var validationResult = ValidationResult.Parser.ParseFrom(
                await ExecuteContractAsync(chainContext, ConsensusConsts.ValidateConsensusAfterExecution,
                    _consensusInformationGenerationService.ParseConsensusTriggerInformation(consensusExtraData), DateTime.UtcNow));

            if (!validationResult.Success)
            {
                Logger.LogError($"Consensus validating after execution failed: {validationResult.Message}");
            }

            return validationResult.Success;
        }

        public async Task<byte[]> GetInformationToUpdateConsensusAsync(ChainContext chainContext)
        {
            return (await ExecuteContractAsync(chainContext, ConsensusConsts.GetInformationToUpdateConsensus,
                _consensusInformationGenerationService.GetTriggerInformation(), _nextMiningTime)).ToByteArray();
            ;
        }

        public async Task<IEnumerable<Transaction>> GenerateConsensusTransactionsAsync(ChainContext chainContext)
        {
            var generatedTransactions = TransactionList.Parser.ParseFrom(
                    await ExecuteContractAsync(chainContext, ConsensusConsts.GenerateConsensusTransactions,
                        _consensusInformationGenerationService.GetTriggerInformation(), _nextMiningTime))
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

        private async Task<ByteString> ExecuteContractAsync(IChainContext chainContext, string consensusMethodName,
            IMessage input, DateTime dateTime)
        {
            var tx = new Transaction
            {
                From = Address.Generate(),
                To = _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider
                    .Name),
                MethodName = consensusMethodName,
                Params = input?.ToByteString() ?? ByteString.Empty
            };

            var transactionTrace =
                await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, tx, dateTime);
            return transactionTrace.ReturnValue;
        }
    }
}