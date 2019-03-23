using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Infrastructure;
using AElf.Kernel.EventMessages;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types.CSharp;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Consensus.Application
{
    public class ConsensusService : IConsensusService
    {
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;

        private readonly IConsensusInformationGenerationService _consensusInformationGenerationService;
        private readonly IAccountService _accountService;
        private readonly IBlockchainService _blockchainService;
        private readonly ConsensusControlInformation _consensusControlInformation;
        private readonly IConsensusScheduler _consensusScheduler;
        private readonly ISmartContractAddressService _smartContractAddressService;
        public ILogger<ConsensusService> Logger { get; set; }

        public ConsensusService(IConsensusInformationGenerationService consensusInformationGenerationService,
            IAccountService accountService, ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            IConsensusScheduler consensusScheduler, IBlockchainService blockchainService,
            ConsensusControlInformation consensusControlInformation,
            ISmartContractAddressService smartContractAddressService)
        {
            _consensusInformationGenerationService = consensusInformationGenerationService;
            _accountService = accountService;
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _blockchainService = blockchainService;
            _consensusControlInformation = consensusControlInformation;
            _smartContractAddressService = smartContractAddressService;
            _consensusScheduler = consensusScheduler;

            Logger = NullLogger<ConsensusService>.Instance;
        }

        public async Task TriggerConsensusAsync()
        {
            // Prepare data for executing contract.
            var address = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            var chainContext = new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            var triggerInformation = _consensusInformationGenerationService.GetTriggerInformation();
            // Upload the consensus command.
            var commandBytes = await ExecuteContractAsync(address, chainContext, ConsensusConsts.GetConsensusCommand,
                triggerInformation);
            _consensusControlInformation.ConsensusCommand =
                ConsensusCommand.Parser.ParseFrom(commandBytes.ToByteArray());

            // Initial consensus scheduler.
            var blockMiningEventData = new BlockMiningEventData(chain.BestChainHash, chain.BestChainHeight,
                _consensusControlInformation.ConsensusCommand.TimeoutMilliseconds);
            _consensusScheduler.CancelCurrentEvent();
            _consensusScheduler.NewEvent(_consensusControlInformation.ConsensusCommand.CountingMilliseconds,
                blockMiningEventData);
        }

        /*
         * TODO Add ConsensusService test cases [Case]
         * BODY 1. ValidateConsensusBeforeExecutionAsync \r\n 2. GetNewConsensusInformationAsync \r\n 3. GenerateConsensusTransactionsAsync
         */
        public async Task<bool> ValidateConsensusBeforeExecutionAsync(Hash preBlockHash, long preBlockHeight,
            byte[] consensusExtraData)
        {
            var address = await _accountService.GetAccountAsync();
            var chainContext = new ChainContext
            {
                BlockHash = preBlockHash,
                BlockHeight = preBlockHeight
            };

            var validationResult = ValidationResult.Parser.ParseFrom(
                await ExecuteContractAsync(address, chainContext, ConsensusConsts.ValidateConsensusBeforeExecution,
                    _consensusInformationGenerationService.ParseConsensusTriggerInformation(consensusExtraData)));

            if (!validationResult.Success)
            {
                Logger.LogError($"Consensus validating before execution failed: {validationResult.Message}");
            }

            return validationResult.Success;
        }

        public async Task<bool> ValidateConsensusAfterExecutionAsync(Hash preBlockHash, long preBlockHeight,
            byte[] consensusExtraData)
        {
            var address = await _accountService.GetAccountAsync();
            var chainContext = new ChainContext
            {
                BlockHash = preBlockHash,
                BlockHeight = preBlockHeight
            };

            var validationResult = ValidationResult.Parser.ParseFrom(
                await ExecuteContractAsync(address, chainContext, ConsensusConsts.ValidateConsensusAfterExecution,
                    _consensusInformationGenerationService.ParseConsensusTriggerInformation(consensusExtraData)));

            if (!validationResult.Success)
            {
                Logger.LogError($"Consensus validating after execution failed: {validationResult.Message}");
            }

            return validationResult.Success;
        }

        public async Task<byte[]> GetNewConsensusInformationAsync()
        {
            var chain = await _blockchainService.GetChainAsync();
            var address = await _accountService.GetAccountAsync();
            var chainContext = new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };

            return (await ExecuteContractAsync(address, chainContext,
                ConsensusConsts.GetNewConsensusInformation,
                _consensusInformationGenerationService.GetTriggerInformation())).ToByteArray();;
        }

        public async Task<IEnumerable<Transaction>> GenerateConsensusTransactionsAsync()
        {
            var chain = await _blockchainService.GetChainAsync();
            var address = await _accountService.GetAccountAsync();
            var chainContext = new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };

            var generatedTransactions = TransactionList.Parser.ParseFrom(
                    await ExecuteContractAsync(address, chainContext, ConsensusConsts.GenerateConsensusTransactions,
                        _consensusInformationGenerationService.GetTriggerInformation()))
                .Transactions
                .ToList();

            // Supply these transactions.
            foreach (var generatedTransaction in generatedTransactions)
            {
                generatedTransaction.RefBlockNumber = chain.BestChainHeight;
                generatedTransaction.RefBlockPrefix = ByteString.CopyFrom(chain.BestChainHash.Value.Take(4).ToArray());
            }

            return generatedTransactions;
        }

        private async Task<ByteString> ExecuteContractAsync(Address fromAddress,
            IChainContext chainContext, string consensusMethodName, IMessage input)
        {
            var tx = new Transaction
            {
                From = fromAddress,
                To = _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider
                    .Name),
                MethodName = consensusMethodName,
                Params = input?.ToByteString() ?? ByteString.Empty
            };

            var transactionTrace =
                await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, tx, DateTime.UtcNow);
            return transactionTrace.ReturnValue;
        }
    }
}