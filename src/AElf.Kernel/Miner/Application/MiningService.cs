using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.Miner.Application
{
    public class MiningService : IMiningService
    {
        public ILogger<MiningService> Logger { get; set; }
        private readonly ISystemTransactionGenerationService _systemTransactionGenerationService;
        private readonly IBlockGenerationService _blockGenerationService;
        private readonly IAccountService _accountService;
        private readonly IBlockExecutingService _blockExecutingService;
        private readonly IBlockchainService _blockchainService;
        private readonly ISystemTransactionExtraDataProvider _systemTransactionExtraDataProvider;

        private readonly EvilTriggerOptions _evilTriggerOptions;

        public ILocalEventBus EventBus { get; set; }

        public MiningService(IAccountService accountService,
            IBlockGenerationService blockGenerationService,
            ISystemTransactionGenerationService systemTransactionGenerationService,
            IBlockExecutingService blockExecutingService,
            IBlockchainService blockchainService,
            ISystemTransactionExtraDataProvider systemTransactionExtraDataProvider,
            IOptionsMonitor<EvilTriggerOptions> evilTriggerOptions)
        {
            Logger = NullLogger<MiningService>.Instance;
            _blockGenerationService = blockGenerationService;
            _systemTransactionGenerationService = systemTransactionGenerationService;
            _blockExecutingService = blockExecutingService;
            _accountService = accountService;
            _blockchainService = blockchainService;
            _systemTransactionExtraDataProvider = systemTransactionExtraDataProvider;

            EventBus = NullLocalEventBus.Instance;

            _evilTriggerOptions = evilTriggerOptions.CurrentValue;
        }

        private async Task<List<Transaction>> GenerateSystemTransactions(Hash previousBlockHash,
            long previousBlockHeight)
        {
            var address = Address.FromPublicKey(await _accountService.GetPublicKeyAsync());
            var systemTransactions = await _systemTransactionGenerationService.GenerateSystemTransactionsAsync(address,
                previousBlockHeight, previousBlockHash);

            foreach (var transaction in systemTransactions)
            {
                await SignAsync(transaction, previousBlockHeight);
            }

            await _blockchainService.AddTransactionsAsync(systemTransactions);

            return systemTransactions;
        }

        private async Task SignAsync(Transaction notSignerTransaction, long previousBlockHeight)
        {
            var signature = await _accountService.SignAsync(notSignerTransaction.GetHash().ToByteArray());
            if (_evilTriggerOptions.ErrorSignatureInSystemTransaction &&
                (previousBlockHeight + 1) % _evilTriggerOptions.EvilTriggerNumber == 0)
            {
                ECKeyPair keyPair = CryptoHelper.GenerateKeyPair();
                signature = CryptoHelper.SignWithPrivateKey(keyPair.PrivateKey,
                    notSignerTransaction.GetHash().ToByteArray());
                Logger.LogWarning(
                    $"EVIL TRIGGER - Error sign system transactions, public key: {keyPair.PublicKey.ToHex()}, private key {keyPair.PrivateKey.ToHex()}");
            }

            notSignerTransaction.Signature = ByteString.CopyFrom(signature);
        }

        /// <summary>
        /// Generate block
        /// </summary>
        /// <returns></returns>
        private async Task<Block> GenerateBlock(Hash preBlockHash, long preBlockHeight, Timestamp expectedMiningTime)
        {
            var block = await _blockGenerationService.GenerateBlockBeforeExecutionAsync(new GenerateBlockDto
            {
                PreviousBlockHash = preBlockHash,
                PreviousBlockHeight = preBlockHeight,
                BlockTime = expectedMiningTime
            });
            block.Header.SignerPubkey = ByteString.CopyFrom(await _accountService.GetPublicKeyAsync());
            if (!_evilTriggerOptions.ChangeBlockHeaderSignPubKey ||
                (preBlockHeight + 1) % _evilTriggerOptions.EvilTriggerNumber != 0) return block;

            ECKeyPair keyPair = CryptoHelper.GenerateKeyPair();
            block.Header.SignerPubkey = ByteString.CopyFrom(keyPair.PublicKey);
            Logger.LogWarning(
                $"EVIL TRIGGER - Error block SignerPubkey, public key: {keyPair.PublicKey.ToHex()}, private key {keyPair.PrivateKey.ToHex()}");

            return block;
        }

        private async Task SignBlockAsync(Block block)
        {
            var signature = await _accountService.SignAsync(block.GetHash().ToByteArray());

            if (_evilTriggerOptions.ErrorSignatureInBlock && block.Height % _evilTriggerOptions.EvilTriggerNumber == 0)
            {
                ECKeyPair keyPair = CryptoHelper.GenerateKeyPair();
                signature = CryptoHelper.SignWithPrivateKey(keyPair.PrivateKey, block.GetHash().ToByteArray());
                Logger.LogWarning(
                    $"EVIL TRIGGER - Error sign block public key: {keyPair.PublicKey.ToHex()}, private key {keyPair.PrivateKey.ToHex()}");
            }

            block.Header.Signature = ByteString.CopyFrom(signature);
        }

        public async Task<BlockExecutedSet> MineAsync(RequestMiningDto requestMiningDto, List<Transaction> transactions,
            Timestamp blockTime)
        {
            try
            {
                using (var cts = new CancellationTokenSource())
                {
                    var expirationTime = blockTime + requestMiningDto.BlockExecutionTime;
                    if (expirationTime < TimestampHelper.GetUtcNow())
                        cts.Cancel();
                    else
                    {
                        var ts = (expirationTime - TimestampHelper.GetUtcNow()).ToTimeSpan();
                        if (ts.TotalMilliseconds > int.MaxValue)
                        {
                            ts = TimeSpan.FromMilliseconds(int.MaxValue);
                        }

                        cts.CancelAfter(ts);
                    }

                    var block = await GenerateBlock(requestMiningDto.PreviousBlockHash,
                        requestMiningDto.PreviousBlockHeight, blockTime);
                    var systemTransactions = await GenerateSystemTransactions(requestMiningDto.PreviousBlockHash,
                        requestMiningDto.PreviousBlockHeight);
                    _systemTransactionExtraDataProvider.SetSystemTransactionCount(systemTransactions.Count,
                        block.Header);
                    var txTotalCount = transactions.Count + systemTransactions.Count;

                    var pending = txTotalCount > requestMiningDto.TransactionCountLimit
                        ? transactions
                            .Take(requestMiningDto.TransactionCountLimit - systemTransactions.Count)
                            .ToList()
                        : transactions;

                    if (transactions.Count > 0 && _evilTriggerOptions.InvalidContracts &&
                        block.Height % _evilTriggerOptions.EvilTriggerNumber == 0)
                    {
                        var tx = transactions.Last();
                        var invalidTransaction = new Transaction
                        {
                            From = tx.From,
                            Params = tx.Params,
                            MethodName = tx.MethodName,
                            RefBlockNumber = tx.RefBlockNumber,
                            RefBlockPrefix = tx.RefBlockPrefix,
                            Signature = tx.Signature
                        };
                        ECKeyPair keyPair = CryptoHelper.GenerateKeyPair();
                        invalidTransaction.To = Address.FromPublicKey(keyPair.PublicKey);
                        if (pending.Count == requestMiningDto.TransactionCountLimit)
                        {
                            pending.RemoveAt(pending.Count - 1);
                            pending.Add(invalidTransaction);
                        }

                        Logger.LogWarning(
                            $"EVIL TRIGGER - InvalidContracts - Contract: {pending.Last().To.ToBase58()}");
                    }

                    if (transactions.Count > 0 && _evilTriggerOptions.InvalidMethod &&
                        block.Height % _evilTriggerOptions.EvilTriggerNumber == 0)
                    {
                        var fakeMethod = "FakeMethod";
                        var tx = transactions.Last();
                        var invalidTransaction = new Transaction
                        {
                            From = tx.From,
                            To = tx.To,
                            Params = tx.Params,
                            MethodName = fakeMethod,
                            RefBlockNumber = tx.RefBlockNumber,
                            RefBlockPrefix = tx.RefBlockPrefix,
                            Signature = tx.Signature
                        };
                        if (pending.Count == requestMiningDto.TransactionCountLimit)
                        {
                            pending.RemoveAt(pending.Count - 1);
                            pending.Add(invalidTransaction);
                        }

                        Logger.LogWarning(
                            $"EVIL TRIGGER - InvalidMethod - Method: {pending.Last().MethodName}");
                    }

                    if (transactions.Count > 0 && _evilTriggerOptions.InvalidSignature &&
                        block.Height % _evilTriggerOptions.EvilTriggerNumber == 0)
                    {
                        var tx = transactions.Last();
                        var invalidTransaction = new Transaction
                        {
                            From = tx.From,
                            To = tx.To,
                            Params = tx.Params,
                            MethodName = tx.MethodName,
                            RefBlockNumber = tx.RefBlockNumber,
                            RefBlockPrefix = tx.RefBlockPrefix,
                        };
                        ECKeyPair keyPair = CryptoHelper.GenerateKeyPair();
                        invalidTransaction.Signature = ByteString.CopyFrom(
                            CryptoHelper.SignWithPrivateKey(keyPair.PrivateKey, block.GetHash().ToByteArray()));
                        if (pending.Count == requestMiningDto.TransactionCountLimit)
                        {
                            pending.RemoveAt(pending.Count - 1);
                            pending.Add(invalidTransaction);
                        }
                        
                        Logger.LogWarning(
                            "EVIL TRIGGER - InvalidSignature");
                    }

                    var blockExecutedSet = await _blockExecutingService.ExecuteBlockAsync(block.Header,
                        systemTransactions, pending, cts.Token);
                    block = blockExecutedSet.Block;

                    if (_evilTriggerOptions.ErrorTransactionCountInBody &&
                        block.Height % _evilTriggerOptions.EvilTriggerNumber == 0)
                    {
                        var last = block.Body.TransactionIds.Last();
                        block.Body.TransactionIds.Add(last);
                        Logger.LogWarning($"EVIL TRIGGER - RepeatTransactionInOneBlockAttack - Tx {last}");
                    }

                    if (_evilTriggerOptions.RemoveOneTransaction &&
                        block.Height % _evilTriggerOptions.EvilTriggerNumber == 0)
                    {
                        Logger.LogWarning(
                            $"EVIL TRIGGER - RemoveTransactions - before remove {block.Body.TransactionsCount}");
                        var last = block.Body.TransactionIds.Last();
                        block.Body.TransactionIds.Remove(last);
                        Logger.LogWarning(
                            $"EVIL TRIGGER - RemoveTransactions - remove Tx {last}, count {block.Body.TransactionsCount}");
                    }

                    if (_evilTriggerOptions.ReverseTransactionList &&
                        block.Height % _evilTriggerOptions.EvilTriggerNumber == 0)
                    {
                        var first = block.Body.TransactionIds.First();
                        Logger.LogWarning($"EVIL TRIGGER - ReverseTransaction - before reverse first Tx {first}");

                        var enumerable = block.Body.TransactionIds.Reverse();
                        var values = enumerable as Hash[] ?? enumerable.ToArray();
                        block.Body.TransactionIds.Clear();
                        block.Body.TransactionIds.Add(values);
                        Logger.LogWarning(
                            $"EVIL TRIGGER - RemoveTransactions - reverse Tx first Tx {block.Body.TransactionIds.First()}");
                    }

                    if (_evilTriggerOptions.ChangeBlockHeader)
                    {
                        var blockHeader = block.Header;
                        var number = _evilTriggerOptions.EvilTriggerNumber;
                        switch (block.Height % number)
                        {
                            case 0:
                                blockHeader.Height += 5;
                                Logger.LogWarning(
                                    $"EVIL TRIGGER - ChangeBlockHeader - Block Height {blockHeader.Height}");
                                break;
                            case 1:
                                blockHeader.MerkleTreeRootOfTransactions =
                                    HashHelper.ComputeFrom("Fake MerkleTreeRootOfTransactions");
                                Logger.LogWarning(
                                    "EVIL TRIGGER - ChangeBlockHeader - Fake MerkleTreeRootOfTransactions");
                                break;
                            case 2:
                                blockHeader.ChainId = ChainHelper.GetChainId(1);
                                Logger.LogWarning(
                                    $"EVIL TRIGGER - ChangeBlockHeader - Error ChainId {blockHeader.ChainId}");
                                break;
                        }
                    }

                    await SignBlockAsync(block);
                    Logger.LogInformation($"Generated block: {block.ToDiagnosticString()}, " +
                                          $"previous: {block.Header.PreviousBlockHash}, " +
                                          $"executed transactions: {block.Body.TransactionsCount}, " +
                                          $"not executed transactions {pending.Count + systemTransactions.Count - block.Body.TransactionsCount}, " +
                                          $"chain id is {block.Header.ChainId}");
                    return blockExecutedSet;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed while mining block.");
                throw;
            }
        }
    }
}