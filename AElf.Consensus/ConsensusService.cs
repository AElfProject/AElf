using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Execution.Execution;
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
using Volo.Abp.Threading;

namespace AElf.Consensus
{
    public class ConsensusService : IConsensusService
    {
        private readonly IConsensusObserver _consensusObserver;

        private readonly ITransactionExecutingService _transactionExecutingService;

        private readonly IConsensusInformationGenerationService _consensusInformationGenerationService;
        private readonly IAccountService _accountService;
        private readonly IBlockchainService _blockchainService;

        private IDisposable _consensusObservables;

        private byte[] _latestGeneratedConsensusInformation;

        public ILogger<ConsensusService> Logger { get; set; }

        public ConsensusService(IConsensusObserver consensusObserver, IExecutingService executingService,
            IConsensusInformationGenerationService consensusInformationGenerationService,
            IAccountService accountService, ITransactionExecutingService transactionExecutingService,
            IBlockchainService blockchainService)
        {
            _consensusObserver = consensusObserver;
            _consensusInformationGenerationService = consensusInformationGenerationService;
            _accountService = accountService;
            _transactionExecutingService = transactionExecutingService;
            _blockchainService = blockchainService;

            Logger = NullLogger<ConsensusService>.Instance;
        }

        public async Task TriggerConsensusAsync(int chainId)
        {            
            var chain = await _blockchainService.GetChainAsync(chainId);
            var chainContext = new ChainContext
            {
                ChainId = chainId,
                BlockHash = chain?.BestChainHash ?? Hash.Genesis,
                BlockHeight = chain?.BestChainHeight ?? 1
            };
            
            var consensusCommand = (await ExecuteContractAsync(chainId, await _accountService.GetAccountAsync(),
                chainContext, ConsensusConsts.GetConsensusCommand, Timestamp.FromDateTime(DateTime.UtcNow))).ToByteArray();

            // Initial or update the schedule.
            _consensusObservables = _consensusObserver.Subscribe(consensusCommand);

            _consensusInformationGenerationService.UpdateConsensusCommand(consensusCommand);
        }

        public async Task<bool> ValidateConsensusAsync(int chainId, Hash preBlockHash, ulong preBlockHeight,
            byte[] consensusInformation)
        {
            var chainContext = new ChainContext
            {
                ChainId = chainId,
                BlockHash = preBlockHash,
                BlockHeight = preBlockHeight
            };
            
            return (await ExecuteContractAsync(chainId, await _accountService.GetAccountAsync(),
                    chainContext, ConsensusConsts.ValidateConsensus, consensusInformation))
                .DeserializeToPbMessage<ValidationResult>().Success;
        }

        public async Task<byte[]> GetNewConsensusInformationAsync(int chainId)
        {
            var chain = await _blockchainService.GetChainAsync(chainId);
            var chainContext = new ChainContext
            {
                ChainId = chainId,
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };

            var newConsensusInformation = (await ExecuteContractAsync(chainId, await _accountService.GetAccountAsync(),
                chainContext, ConsensusConsts.GetNewConsensusInformation,
                _consensusInformationGenerationService.GenerateExtraInformation())).DeserializeToBytes();

            _latestGeneratedConsensusInformation = newConsensusInformation;

            return newConsensusInformation;
        }

        public async Task<IEnumerable<Transaction>> GenerateConsensusTransactionsAsync(int chainId, ulong refBlockHeight,
            byte[] refBlockPrefix)
        {
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

        private async Task<ByteString> ExecuteContractAsync(int chainId, Address fromAddress, IChainContext chainContext,
            string consensusMethodName, params object[] objects)
        {
            var tx = new Transaction
            {
                From = fromAddress,
                To = ContractHelpers.GetConsensusContractAddress(chainId),
                MethodName = consensusMethodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(objects))
            };

            var executionReturnSets = await _transactionExecutingService.ExecuteAsync(chainId, chainContext,
                new List<Transaction> {tx},
                DateTime.UtcNow, new CancellationToken());
            return executionReturnSets.Last().ReturnValue;
        }
    }
}