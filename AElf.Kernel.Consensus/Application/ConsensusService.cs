using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Infrastructure;
using AElf.Kernel.EventMessages;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Consensus.Application
{
    public class ConsensusService : IConsensusService
    {
        private readonly ITransactionExecutingService _transactionExecutingService;

        private readonly IConsensusInformationGenerationService _consensusInformationGenerationService;
        private readonly IAccountService _accountService;
        private readonly IBlockchainService _blockchainService;
        private readonly ConsensusControlInformation _consensusControlInformation;
        private readonly IConsensusScheduler _consensusScheduler;

        private byte[] _latestGeneratedConsensusInformation;

        public ILogger<ConsensusService> Logger { get; set; }

        public ConsensusService(IConsensusInformationGenerationService consensusInformationGenerationService,
            IAccountService accountService, ITransactionExecutingService transactionExecutingService,
            IConsensusScheduler consensusScheduler, IBlockchainService blockchainService,
            ConsensusControlInformation consensusControlInformation)
        {
            _consensusInformationGenerationService = consensusInformationGenerationService;
            _accountService = accountService;
            _transactionExecutingService = transactionExecutingService;
            _blockchainService = blockchainService;
            _consensusControlInformation = consensusControlInformation;
            _consensusScheduler = consensusScheduler;

            Logger = NullLogger<ConsensusService>.Instance;
        }

        public async Task TriggerConsensusAsync(int chainId)
        {
            var chain = await _blockchainService.GetChainAsync(chainId);

            var chainContext = new ChainContext
            {
                ChainId = chainId,
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };

            _consensusControlInformation.ConsensusCommand = ConsensusCommand.Parser.ParseFrom(
                (await ExecuteContractAsync(chainId,
                    await _accountService.GetAccountAsync(), chainContext, ConsensusConsts.GetConsensusCommand,
                    _consensusInformationGenerationService.GetFirstExtraInformation()))
                .ToByteArray());

            var blockMiningEventData = new BlockMiningEventData(chainId, chain.BestChainHash, chain.BestChainHeight,
                DateTime.UtcNow.AddMilliseconds(_consensusControlInformation.ConsensusCommand.TimeoutMilliseconds));

            // Initial or reload consensus scheduler.
            _consensusScheduler.CancelCurrentEvent();
            _consensusScheduler.NewEvent(_consensusControlInformation.ConsensusCommand.CountingMilliseconds,
                blockMiningEventData);
        }

        public async Task<bool> ValidateConsensusAsync(int chainId, Hash preBlockHash, ulong preBlockHeight,
            byte[] consensusInformation)
        {
            Logger.LogInformation("Generating consensus transactions.");

            var chainContext = new ChainContext
            {
                ChainId = chainId,
                BlockHash = preBlockHash,
                BlockHeight = preBlockHeight
            };

            var validationResult = (await ExecuteContractAsync(chainId, await _accountService.GetAccountAsync(),
                    chainContext, ConsensusConsts.ValidateConsensus, consensusInformation))
                .DeserializeToPbMessage<ValidationResult>();

            if (!validationResult.Success)
            {
                Logger.LogError($"Consensus validating failed: {validationResult.Message}");
            }

            return validationResult.Success;
        }

        public async Task<byte[]> GetNewConsensusInformationAsync(int chainId)
        {
            Logger.LogInformation("Getting new consensus information.");
            
            var chain = await _blockchainService.GetChainAsync(chainId);
            var chainContext = new ChainContext
            {
                ChainId = chainId,
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };

            var newConsensusInformation = (await ExecuteContractAsync(chainId, await _accountService.GetAccountAsync(),
                chainContext, ConsensusConsts.GetNewConsensusInformation,
                _consensusInformationGenerationService.GenerateExtraInformation())).ToByteArray();

            _latestGeneratedConsensusInformation = newConsensusInformation;

            return newConsensusInformation;
        }

        public async Task<IEnumerable<Transaction>> GenerateConsensusTransactionsAsync(int chainId,
            ulong refBlockHeight, byte[] refBlockPrefix)
        {
            Logger.LogInformation("Generating consensus transactions.");

            var chain = await _blockchainService.GetChainAsync(chainId);
            var chainContext = new ChainContext
            {
                ChainId = chainId,
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };

            var generatedTransactions = (await ExecuteContractAsync(chainId, await _accountService.GetAccountAsync(),
                    chainContext, ConsensusConsts.GenerateConsensusTransactions, refBlockHeight, refBlockPrefix,
                    _consensusInformationGenerationService.GenerateExtraInformationForTransaction(
                        _latestGeneratedConsensusInformation, chainId))).DeserializeToPbMessage<TransactionList>()
                .Transactions
                .ToList();

            return generatedTransactions;
        }

        private async Task<ByteString> ExecuteContractAsync(int chainId, Address fromAddress,
            IChainContext chainContext, string consensusMethodName, params object[] objects)
        {
            var tx = new Transaction
            {
                From = fromAddress,
                To = Address.BuildContractAddress(chainId, 1),
                MethodName = consensusMethodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(objects))
            };

            var executionReturnSets = await _transactionExecutingService.ExecuteAsync(chainContext,
                new List<Transaction> {tx},
                DateTime.UtcNow, new CancellationToken());
            return executionReturnSets.Last().ReturnValue;
        }
    }
}