using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public sealed class TxHubTests : TransactionPoolTxHubTestBase
    {
        private readonly ITxHub _txHub;
        private readonly IBlockchainService _blockchainService;
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly ILocalEventBus _eventBus;

        public TxHubTests()
        {
            _txHub = GetRequiredService<ITxHub>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _eventBus = GetRequiredService<ILocalEventBus>();
        }

        [Fact]
        public async Task TxHub_Test()
        {
            TransactionValidationStatusChangedEvent transactionValidationStatusChangedEventData = null;
            _eventBus.Subscribe<TransactionValidationStatusChangedEvent>(d =>
            {
                transactionValidationStatusChangedEventData = d;
                return Task.CompletedTask;
            });

            TransactionAcceptedEvent transactionAcceptedEvent = null;
            _eventBus.Subscribe<TransactionAcceptedEvent>(d =>
            {
                transactionAcceptedEvent = d;
                return Task.CompletedTask;
            });

            {
                // Empty transaction pool
                // Chain:
                //         BestChainHeight: 11
                // TxHub:
                //         BestChainHeight: 0
                //          AllTransaction: 0
                //    ValidatedTransaction: 0
                await TxHubBestChainShouldBeAsync(Hash.Empty, 0);
                await TransactionPoolSizeShouldBeAsync(0, 0);
                transactionValidationStatusChangedEventData.ShouldBeNull();
                transactionAcceptedEvent.ShouldBeNull();
            }

            {
                // Receive a valid transaction 
                // Chain:
                //         BestChainHeight: 11
                // TxHub:
                //         BestChainHeight: 0
                //          AllTransaction: 0
                //    ValidatedTransaction: 0
                var chain = await _blockchainService.GetChainAsync();
                var transactionValid =
                    _kernelTestHelper.GenerateTransaction(chain.BestChainHeight, chain.BestChainHash);

                await AddTransactionsAsync(new List<Transaction> {transactionValid});
                await TxHubBestChainShouldBeAsync(Hash.Empty, 0);
                await TransactionPoolSizeShouldBeAsync(0, 0);
                await CheckTransactionInPoolAsync(transactionValid, false);
                transactionValidationStatusChangedEventData.ShouldBeNull();
                transactionAcceptedEvent.ShouldBeNull();
            }

            {
                // Receive best chain found event
                // Chain:
                //         BestChainHeight: 12
                // TxHub:
                //         BestChainHeight: 12
                //          AllTransaction: 0
                //    ValidatedTransaction: 0
                await _kernelTestHelper.AttachBlockToBestChain();
                var chain = await _blockchainService.GetChainAsync();
                await _txHub.UpdateTransactionPoolByBestChainAsync(chain.BestChainHash, chain.BestChainHeight);
                await TxHubBestChainShouldBeAsync(chain.BestChainHash, 12);
                await TransactionPoolSizeShouldBeAsync(0, 0);
            }

            {
                // Receive a different branch transaction
                // Chain:
                //         BestChainHeight: 12
                // TxHub:
                //         BestChainHeight: 12
                //          AllTransaction: 1
                //    ValidatedTransaction: 0
                var transactionDifferent = _kernelTestHelper.GenerateTransaction(12);
                var chain = await _blockchainService.GetChainAsync();
                await AddTransactionsAsync(new List<Transaction> {transactionDifferent});
                await TxHubBestChainShouldBeAsync(chain.BestChainHash, 12);
                await TransactionPoolSizeShouldBeAsync(1, 0);
                await CheckTransactionInPoolAsync(transactionDifferent, true);
                transactionValidationStatusChangedEventData.ShouldBeNull();
                transactionAcceptedEvent.ShouldBeNull();
            }

            {
                // Receive a future transaction
                // Chain:
                //         BestChainHeight: 12
                // TxHub:
                //         BestChainHeight: 12
                //          AllTransaction: 1
                //    ValidatedTransaction: 0
                var chain = await _blockchainService.GetChainAsync();
                var transactionFuture = _kernelTestHelper.GenerateTransaction(chain.BestChainHeight + 1);
                await AddTransactionsAsync(new List<Transaction> {transactionFuture});
                await TxHubBestChainShouldBeAsync(chain.BestChainHash, 12);
                await TransactionPoolSizeShouldBeAsync(1, 0);
                await CheckTransactionInPoolAsync(transactionFuture, false);

                transactionAcceptedEvent.ShouldBeNull();
                transactionValidationStatusChangedEventData.ShouldNotBeNull();
                transactionValidationStatusChangedEventData.TransactionId.ShouldBe(transactionFuture.GetHash());
                transactionValidationStatusChangedEventData.TransactionResultStatus.ShouldBe(TransactionResultStatus
                    .NodeValidationFailed);
                transactionValidationStatusChangedEventData.Error.ShouldContain("Transaction expired");

                transactionValidationStatusChangedEventData = null;
            }

            {
                // Receive a invalid transaction
                // Chain:
                //         BestChainHeight: 12
                // TxHub:
                //         BestChainHeight: 12
                //          AllTransaction: 1
                //    ValidatedTransaction: 0
                var chain = await _blockchainService.GetChainAsync();
                var transactionInvalid =
                    _kernelTestHelper.GenerateTransaction(chain.BestChainHeight, chain.BestChainHash);
                transactionInvalid.Signature = ByteString.Empty;
                await AddTransactionsAsync(new List<Transaction> {transactionInvalid});
                await TxHubBestChainShouldBeAsync(chain.BestChainHash, 12);
                await TransactionPoolSizeShouldBeAsync(1, 0);
                await CheckTransactionInPoolAsync(transactionInvalid, false);

                transactionValidationStatusChangedEventData.ShouldNotBeNull();
                transactionValidationStatusChangedEventData.TransactionId.ShouldBe(transactionInvalid.GetHash());
                transactionValidationStatusChangedEventData.TransactionResultStatus.ShouldBe(TransactionResultStatus
                    .NodeValidationFailed);
                transactionValidationStatusChangedEventData.Error.ShouldBe("Incorrect transaction signature.");
                transactionAcceptedEvent.ShouldBeNull();

                transactionValidationStatusChangedEventData = null;
            }

            {
                // Receive a transaction already in DB
                // Chain:
                //         BestChainHeight: 12
                // TxHub:
                //         BestChainHeight: 12
                //          AllTransaction: 1
                //    ValidatedTransaction: 0
                var chain = await _blockchainService.GetChainAsync();
                var transactionInDB =
                    _kernelTestHelper.GenerateTransaction(chain.BestChainHeight, chain.BestChainHash);
                await _blockchainService.AddTransactionsAsync(new List<Transaction> {transactionInDB});
                await AddTransactionsAsync(new List<Transaction> {transactionInDB});
                await TxHubBestChainShouldBeAsync(chain.BestChainHash, 12);
                await TransactionPoolSizeShouldBeAsync(1, 0);
                await CheckTransactionInPoolAsync(transactionInDB, false);
                transactionValidationStatusChangedEventData.ShouldBeNull();
                transactionAcceptedEvent.ShouldBeNull();
            }

            {
                // Receive a valid transaction 
                // Chain:
                //         BestChainHeight: 12
                // TxHub:
                //         BestChainHeight: 12
                //          AllTransaction: 2
                //    ValidatedTransaction: 1
                var chain = await _blockchainService.GetChainAsync();
                var transactionValid =
                    _kernelTestHelper.GenerateTransaction(chain.BestChainHeight, chain.BestChainHash);

                await AddTransactionsAsync(new List<Transaction> {transactionValid});
                await _txHub.UpdateTransactionPoolByBestChainAsync(chain.BestChainHash, chain.BestChainHeight);
                await TxHubBestChainShouldBeAsync(chain.BestChainHash, 12);
                await TransactionPoolSizeShouldBeAsync(2, 1);
                await CheckTransactionInPoolAsync(transactionValid, true);
                transactionValidationStatusChangedEventData.ShouldBeNull();
                transactionAcceptedEvent.ShouldNotBeNull();
                transactionAcceptedEvent.Transaction.ShouldBe(transactionValid);
                transactionAcceptedEvent.ChainId.ShouldBe(chain.Id);

                transactionAcceptedEvent = null;

                // Receive the valid transaction again
                // Chain:
                //         BestChainHeight: 12
                // TxHub:
                //         BestChainHeight: 12
                //          AllTransaction: 2
                //    ValidatedTransaction: 1
                await AddTransactionsAsync(new List<Transaction> {transactionValid});
                await TxHubBestChainShouldBeAsync(chain.BestChainHash, 12);
                await TransactionPoolSizeShouldBeAsync(2, 1);
                await CheckTransactionInPoolAsync(transactionValid, true);
                transactionValidationStatusChangedEventData.ShouldBeNull();
                transactionAcceptedEvent.ShouldBeNull();

                // Mined a block
                // Chain:
                //         BestChainHeight: 13
                // TxHub:
                //         BestChainHeight: 12
                //          AllTransaction: 1
                //    ValidatedTransaction: 0
                var transactionNotInPool =
                    _kernelTestHelper.GenerateTransaction(chain.BestChainHeight, chain.BestChainHash);

                var newBlock = await _kernelTestHelper.AttachBlockToBestChain(new List<Transaction>
                {
                    transactionValid,
                    transactionNotInPool
                });

                await _txHub.CleanByTransactionIdsAsync(newBlock.TransactionIds);
                await TxHubBestChainShouldBeAsync(chain.BestChainHash, 12);
                await TransactionPoolSizeShouldBeAsync(1, 1);
                await CheckTransactionInPoolAsync(transactionValid, false);
            }

            {
                // Receive best chain found event
                // Chain:
                //         BestChainHeight: 13
                // TxHub:
                //         BestChainHeight: 13
                //          AllTransaction: 1
                //    ValidatedTransaction: 0
                var chain = await _blockchainService.GetChainAsync();

                await _txHub.UpdateTransactionPoolByBestChainAsync(chain.BestChainHash, chain.BestChainHeight);

                await TxHubBestChainShouldBeAsync(chain.BestChainHash, 13);
                await TransactionPoolSizeShouldBeAsync(1, 0);
            }

            {
                // Receive a valid transaction and a expired transaction
                // Chain:
                //         BestChainHeight: 13
                // TxHub:
                //         BestChainHeight: 13
                //          AllTransaction: 2
                //    ValidatedTransaction: 1
                var chain = await _blockchainService.GetChainAsync();
                var transactionValid =
                    _kernelTestHelper.GenerateTransaction(chain.BestChainHeight, chain.BestChainHash);
                var transactionExpired = _kernelTestHelper.GenerateTransaction(chain.BestChainHeight + 100);

                await AddTransactionsAsync(new List<Transaction>
                {
                    transactionValid,
                    transactionExpired
                });

                await _txHub.UpdateTransactionPoolByBestChainAsync(chain.BestChainHash, chain.BestChainHeight);
                await TxHubBestChainShouldBeAsync(chain.BestChainHash, 13);
                await TransactionPoolSizeShouldBeAsync(2, 1);
                await CheckTransactionInPoolAsync(transactionValid, true);
                await CheckTransactionInPoolAsync(transactionExpired, false);

                transactionValidationStatusChangedEventData.ShouldNotBeNull();
                transactionValidationStatusChangedEventData.TransactionId.ShouldBe(transactionExpired.GetHash());
                transactionValidationStatusChangedEventData.TransactionResultStatus.ShouldBe(TransactionResultStatus
                    .NodeValidationFailed);
                transactionValidationStatusChangedEventData.Error.ShouldContain("Transaction expired");

                transactionAcceptedEvent.ShouldNotBeNull();
                transactionAcceptedEvent.Transaction.ShouldBe(transactionValid);
                transactionAcceptedEvent.ChainId.ShouldBe(chain.Id);

                transactionAcceptedEvent = null;
                transactionValidationStatusChangedEventData = null;
            }

            {
                // Set lib 12
                // Chain:
                //         BestChainHeight: 13
                // TxHub:
                //         BestChainHeight: 13
                //          AllTransaction: 1
                //    ValidatedTransaction: 1
                var chain = await _blockchainService.GetChainAsync();

                await _txHub.CleanByHeightAsync(12);
                await TxHubBestChainShouldBeAsync(chain.BestChainHash, 13);
                await TransactionPoolSizeShouldBeAsync(1, 1);
            }

            {
                // After 513 blocks
                // Chain:
                //         BestChainHeight: 526
                // TxHub:
                //         BestChainHeight: 526
                //          AllTransaction: 1
                //    ValidatedTransaction: 0
                var chain = await _blockchainService.GetChainAsync();
                var bestChainHeight = chain.BestChainHeight;
                for (var i = 0; i < KernelConstants.ReferenceBlockValidPeriod + 1; i++)
                {
                    var transaction = _kernelTestHelper.GenerateTransaction(bestChainHeight + i);
                    await _kernelTestHelper.AttachBlockToBestChain(new List<Transaction> {transaction});
                    chain = await _blockchainService.GetChainAsync();
                    await _txHub.UpdateTransactionPoolByBestChainAsync(chain.BestChainHash, chain.BestChainHeight);
                }

                await TxHubBestChainShouldBeAsync(chain.BestChainHash, 526);

                await TransactionPoolSizeShouldBeAsync(0, 0);
            }
        }

        [Fact]
        public async Task UpdateTransactionPoolByBestChain_Test()
        {
            var chain = await _blockchainService.GetChainAsync();
            await _txHub.UpdateTransactionPoolByBestChainAsync(chain.BestChainHash, chain.BestChainHeight);
            var transaction =
                _kernelTestHelper.GenerateTransaction(_kernelTestHelper.LongestBranchBlockList[2].Height,
                    _kernelTestHelper.LongestBranchBlockList[2].GetHash());
            await AddTransactionsAsync(new[] {transaction});
            await _txHub.UpdateTransactionPoolByBestChainAsync(chain.BestChainHash, chain.BestChainHeight);
            
            await TransactionPoolSizeShouldBeAsync(1, 0);

            await _blockchainService.SetBestChainAsync(chain, _kernelTestHelper.LongestBranchBlockList.Last().Height,
                _kernelTestHelper.LongestBranchBlockList.Last().GetHash());
            chain = await _blockchainService.GetChainAsync();
            await _txHub.UpdateTransactionPoolByBestChainAsync(chain.BestChainHash, chain.BestChainHeight);
            
            await TransactionPoolSizeShouldBeAsync(1, 1);
        }

        [Fact]
        public async Task AddTransactions_Limit_Test()
        {
            TransactionValidationStatusChangedEvent transactionValidationStatusChangedEventData = null;
            _eventBus.Subscribe<TransactionValidationStatusChangedEvent>(d =>
            {
                transactionValidationStatusChangedEventData = d;
                return Task.CompletedTask;
            });
            
            var chain = await _blockchainService.GetChainAsync();
            await _txHub.UpdateTransactionPoolByBestChainAsync(chain.BestChainHash, chain.BestChainHeight);
            for (var i = 0; i < 20; i++)
            {
                var tx = _kernelTestHelper.GenerateTransaction(chain.BestChainHeight, chain.BestChainHash);
                await AddTransactionsAsync(new[] {tx});
            }
            await _txHub.UpdateTransactionPoolByBestChainAsync(chain.BestChainHash, chain.BestChainHeight);
            await TransactionPoolSizeShouldBeAsync(20, 20);

            var transaction = _kernelTestHelper.GenerateTransaction(chain.BestChainHeight, chain.BestChainHash);
            await AddTransactionsAsync(new[] {transaction});
            await TransactionPoolSizeShouldBeAsync(20, 20);
                
            transactionValidationStatusChangedEventData.ShouldNotBeNull();
            transactionValidationStatusChangedEventData.TransactionId.ShouldBe(transaction.GetHash());
            transactionValidationStatusChangedEventData.TransactionResultStatus.ShouldBe(TransactionResultStatus
                .NodeValidationFailed);
            transactionValidationStatusChangedEventData.Error.ShouldBe("Transaction Pool is full.");
        }

        [Fact]
        public async Task GetExecutableTransactionSet_Test()
        {
            var chain = await _blockchainService.GetChainAsync();

            await _txHub.UpdateTransactionPoolByBestChainAsync(chain.BestChainHash, chain.BestChainHeight);
            
            var transaction =
                _kernelTestHelper.GenerateTransaction(chain.BestChainHeight, chain.BestChainHash);
            await AddTransactionsAsync(new List<Transaction>
            {
                transaction
            });

            await _txHub.UpdateTransactionPoolByBestChainAsync(chain.BestChainHash, chain.BestChainHeight);

            var executableTransactionSet = await _txHub.GetExecutableTransactionSetAsync(chain.BestChainHash, 0);
            executableTransactionSet.PreviousBlockHash.ShouldBe(chain.BestChainHash);
            executableTransactionSet.PreviousBlockHeight.ShouldBe(chain.BestChainHeight);
            executableTransactionSet.Transactions.Count.ShouldBe(0);
            
            executableTransactionSet = await _txHub.GetExecutableTransactionSetAsync(chain.BestChainHash, int.MaxValue);
            executableTransactionSet.PreviousBlockHash.ShouldBe(chain.BestChainHash);
            executableTransactionSet.PreviousBlockHeight.ShouldBe(chain.BestChainHeight);
            executableTransactionSet.Transactions[0].ShouldBe(transaction);

            var wrongBlockHash = HashHelper.ComputeFrom("WrongBlockHash");
            executableTransactionSet = await _txHub.GetExecutableTransactionSetAsync(wrongBlockHash, int.MaxValue);
            executableTransactionSet.PreviousBlockHash.ShouldBe(chain.BestChainHash);
            executableTransactionSet.PreviousBlockHeight.ShouldBe(chain.BestChainHeight);
            executableTransactionSet.Transactions.Count.ShouldBe(0);
        }

        private async Task AddTransactionsAsync(IEnumerable<Transaction> transactions)
        {
            await _txHub.AddTransactionsAsync(transactions);
            await Task.Delay(200);
        }

        #region check methods

        private async Task CheckTransactionInPoolAsync(Transaction transaction, bool isInPool)
        {
            var existQueuedTransaction = await _txHub.GetQueuedTransactionAsync(transaction.GetHash());
            if (isInPool)
            {
                existQueuedTransaction.Transaction.ShouldBe(transaction);
            }
            else
            {
                existQueuedTransaction.ShouldBeNull();
            }
        }

        private async Task TransactionPoolSizeShouldBeAsync(int allTransactionCount, int validatedTransactionCount)
        {
            var transactionPoolStatus = await _txHub.GetTransactionPoolStatusAsync();
            transactionPoolStatus.AllTransactionCount.ShouldBe(allTransactionCount);
            transactionPoolStatus.ValidatedTransactionCount.ShouldBe(validatedTransactionCount);
        }

        private async Task TxHubBestChainShouldBeAsync(Hash blockHash, long blockHeight)
        {
            var executableTxSet = await _txHub.GetExecutableTransactionSetAsync(blockHash, int.MaxValue);
            executableTxSet.PreviousBlockHash.ShouldBe(blockHash);
            executableTxSet.PreviousBlockHeight.ShouldBe(blockHeight);
        }

        #endregion
    }
}