using System.Collections.Generic;
using Acs6;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.Threading;

namespace AElf.Kernel.Consensus.AEDPoS
{
   public class RandomNumberTransactionGenerator : ISystemTransactionGenerator
    {
        private readonly ITransactionGeneratingService _transactionGeneratingService;
        private readonly ITransactionResultService _transactionResultService;
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;

        private readonly Dictionary<long, Hash> _requestedTokens = new Dictionary<long, Hash>();
        private readonly Dictionary<long, List<Hash>> _readyTokens = new Dictionary<long, List<Hash>>();

        public ILogger<RandomNumberTransactionGenerator> Logger { get; set; }

        public RandomNumberTransactionGenerator(ITransactionGeneratingService transactionGeneratingService,
            ITransactionResultService transactionResultService,
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService)
        {
            _transactionGeneratingService = transactionGeneratingService;
            _transactionResultService = transactionResultService;
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;

            Logger = NullLogger<RandomNumberTransactionGenerator>.Instance;
        }

        public void GenerateTransactions(Address @from, long preBlockHeight, Hash preBlockHash,
            ref List<Transaction> generatedTransactions)
        {
            if (preBlockHeight < 10)
            {
                return;
            }

            // Generate tx: RequestRandomNumber
            var requestParam = new Empty().ToByteString();
            var requestTx = AsyncHelper.RunSync(() => _transactionGeneratingService.GenerateTransactionAsync(
                ConsensusSmartContractAddressNameProvider.Name, "RequestRandomNumber", requestParam));
            var transactions = new List<Transaction>
            {
                requestTx
            };

            _requestedTokens[preBlockHeight + 1] = requestTx.GetHash();

            if (_requestedTokens.TryGetValue(preBlockHeight, out var previousRequestTxId))
            {
                var txResult = AsyncHelper.RunSync(() =>
                    _transactionResultService.GetTransactionResultAsync(previousRequestTxId));
                var randomNumberOrder = new RandomNumberOrder();
                randomNumberOrder.MergeFrom(txResult.ReturnValue);
                if (_readyTokens.ContainsKey(randomNumberOrder.BlockHeight))
                {
                    _readyTokens[randomNumberOrder.BlockHeight].Add(randomNumberOrder.TokenHash);
                }
                else
                {
                    _readyTokens[randomNumberOrder.BlockHeight] = new List<Hash> {randomNumberOrder.TokenHash};
                }
                Logger.LogDebug(
                    $"[Boilerplate]Result of {previousRequestTxId.ToHex()}: {txResult.ReadableReturnValue}");
            }

            if (_readyTokens.ContainsKey(preBlockHeight + 1))
            {
                foreach (var tokenHash in _readyTokens[preBlockHeight + 1])
                {
                    var getTx = AsyncHelper.RunSync(() => _transactionGeneratingService.GenerateTransactionAsync(
                        ConsensusSmartContractAddressNameProvider.Name, "GetRandomNumber", tokenHash.ToByteString()));
                    var trace = AsyncHelper.RunSync(() => _transactionReadOnlyExecutionService.ExecuteAsync(
                        new ChainContext {BlockHeight = preBlockHeight, BlockHash = preBlockHash}, getTx,
                        TimestampHelper.GetUtcNow()));
                    var randomNumber = trace.ReadableReturnValue;
                    if (!string.IsNullOrEmpty(randomNumber))
                    {
                        Logger.LogDebug($"[Boilerplate]Random number of {tokenHash.ToHex()}: {randomNumber}");
                    }
                    else
                    {
                        Logger.LogDebug($"[Boilerplate]Cannot get the random number of {tokenHash.ToHex()}");
                    }
                }
            }

            generatedTransactions.AddRange(transactions);
        }
    }
}