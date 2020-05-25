using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Types;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public sealed class TxHubTests : TransactionPoolWithChainTestBase
    {
        private readonly TxHub _txHub;
        private readonly IBlockchainService _blockchainService;
        private readonly KernelTestHelper _kernelTestHelper;

        public TxHubTests()
        {
            _txHub = GetRequiredService<TxHub>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }

        [Fact]
        public async Task Test_TxHub()
        {
            {
                // Empty transaction pool
                // Chain:
                //         BestChainHeight: 11
                // TxHub:
                //         BestChainHeight: 0
                //          AllTransaction: 0
                //   ExecutableTransaction: 0
                ExecutableTransactionShouldBe(Hash.Empty, 0);

                TransactionPoolSizeShouldBe(0);
            }

            var transactionHeight100 = _kernelTestHelper.GenerateTransaction(100);

            {
                // Receive a feature transaction
                // Chain:
                //         BestChainHeight: 11
                // TxHub:
                //         BestChainHeight: 0
                //          AllTransaction: 0
                //   ExecutableTransaction: 0

                // Receive the transaction first time
                await _txHub.AddTransactionsAsync(new List<Transaction> {transactionHeight100});

                ExecutableTransactionShouldBe(Hash.Empty, 0);

                TransactionPoolSizeShouldBe(0); //_bestChainHash = Hash.Empty
            }

            {
                // Receive a valid transaction 
                // Chain:
                //         BestChainHeight: 11
                // TxHub:
                //         BestChainHeight: 0
                //          AllTransaction: 0
                //   ExecutableTransaction: 0
                var chain = await _blockchainService.GetChainAsync();
                var transactionValid =
                    _kernelTestHelper.GenerateTransaction(chain.BestChainHeight, chain.BestChainHash);

                await _txHub.AddTransactionsAsync(new List<Transaction> {transactionValid});

                TransactionPoolSizeShouldBe(0);

                // Receive a block
                // Chain:
                //         BestChainHeight: 12
                // TxHub:
                //         BestChainHeight: 0
                //          AllTransaction: 0
                //   ExecutableTransaction: 0
                var transactionNotInPool =
                    _kernelTestHelper.GenerateTransaction(chain.BestChainHeight, chain.BestChainHash);

                var newBlock = await _kernelTestHelper.AttachBlockToBestChain(new List<Transaction>
                {
                    transactionValid,
                    transactionNotInPool
                });

                await _txHub.CleanByTransactionIdsAsync(newBlock.TransactionIds);

                TransactionPoolSizeShouldBe(0);
            }

            {
                // Receive best chain found event
                // Chain:
                //         BestChainHeight: 12
                // TxHub:
                //         BestChainHeight: 12
                //          AllTransaction: 0
                //   ExecutableTransaction: 0
                var chain = await _blockchainService.GetChainAsync();

                await _txHub.UpdateTransactionPoolByBestChainAsync(chain.BestChainHash, chain.BestChainHeight);

                ExecutableTransactionShouldBe(chain.BestChainHash, chain.BestChainHeight);

                TransactionPoolSizeShouldBe(0);
            }

            {
                // Receive a valid transaction and a invalid transaction
                // Chain:
                //         BestChainHeight: 12
                // TxHub:
                //         BestChainHeight: 12
                //          AllTransaction: 2
                //   ExecutableTransaction: 0
                var chain = await _blockchainService.GetChainAsync();
                var transactionValid =
                    _kernelTestHelper.GenerateTransaction(chain.BestChainHeight, chain.BestChainHash);
                var transactionInvalid = _kernelTestHelper.GenerateTransaction(chain.BestChainHeight - 1);

                await _txHub.AddTransactionsAsync(new List<Transaction>
                {
                    transactionValid,
                    transactionInvalid
                });

                ExecutableTransactionShouldBe(chain.BestChainHash, chain.BestChainHeight);
                await Task.Delay(200);
                TransactionPoolSizeShouldBe(2);
                TransactionShouldInPool(transactionValid);

                // Receive the same transaction again
                await _txHub.AddTransactionsAsync(new List<Transaction> {transactionValid, transactionInvalid});

                ExecutableTransactionShouldBe(chain.BestChainHash, chain.BestChainHeight);
                TransactionPoolSizeShouldBe(2);

                {
                    // Receive a block
                    // Chain:
                    //         BestChainHeight: 13
                    // TxHub:
                    //         BestChainHeight: 13
                    //          AllTransaction: 1
                    //   ExecutableTransaction: 0
                    var transactionNotInPool = _kernelTestHelper.GenerateTransaction(chain.BestChainHeight - 2);

                    var newBlock = await _kernelTestHelper.AttachBlockToBestChain(new List<Transaction>
                    {
                        transactionValid,
                        transactionNotInPool
                    });

                    await _txHub.CleanByTransactionIdsAsync(newBlock.TransactionIds);

                    chain = await _blockchainService.GetChainAsync();

                    //handle best chain found
                    await _txHub.UpdateTransactionPoolByBestChainAsync(chain.BestChainHash, chain.BestChainHeight);

                    TransactionPoolSizeShouldBe(1);
                    TransactionShouldInPool(transactionInvalid);
                }

                // Receive lib found event
                // Chain:
                //         BestChainHeight: 13
                // TxHub:
                //         BestChainHeight: 13
                //          AllTransaction: 0
                //   ExecutableTransaction: 0
                await _txHub.CleanByHeightAsync(chain.BestChainHeight);
                TransactionPoolSizeShouldBe(0);
            }

            {
                // After 513 blocks
                // Chain:
                //         BestChainHeight: 526
                // TxHub:
                //         BestChainHeight: 526
                //          AllTransaction: 0
                //   ExecutableTransaction: 0
                var chain = await _blockchainService.GetChainAsync();
                var bestChainHeight = chain.BestChainHeight;
                for (var i = 0; i < KernelConstants.ReferenceBlockValidPeriod + 1; i++)
                {
                    var transaction = _kernelTestHelper.GenerateTransaction(bestChainHeight + i);
                    await _kernelTestHelper.AttachBlockToBestChain(new List<Transaction> {transaction});
                    chain = await _blockchainService.GetChainAsync();
                    await _txHub.UpdateTransactionPoolByBestChainAsync(chain.BestChainHash, chain.BestChainHeight);
                    await _txHub.CleanByHeightAsync(chain.BestChainHeight);
                }

                ExecutableTransactionShouldBe(chain.BestChainHash, chain.BestChainHeight);

                TransactionPoolSizeShouldBe(0);
            }
        }

        #region check methods

        private void TransactionShouldInPool(Transaction transaction)
        {
            var existQueuedTransaction = _txHub.GetQueuedTransactionAsync(transaction.GetHash()).Result;
            existQueuedTransaction.Transaction.ShouldBe(transaction);
        }

        private void TransactionPoolSizeShouldBe(int size)
        {
            var transactionPoolSize = AsyncHelper.RunSync(_txHub.GetTransactionPoolStatusAsync).AllTransactionCount;
            transactionPoolSize.ShouldBe(size);
        }

        private void ExecutableTransactionShouldBe(Hash previousBlockHash, long previousBlockHeight,
            List<Transaction> transactions = null)
        {
            var executableTxSet = _txHub.GetExecutableTransactionSetAsync().Result;
            executableTxSet.PreviousBlockHash.ShouldBe(previousBlockHash);
            executableTxSet.PreviousBlockHeight.ShouldBe(previousBlockHeight);
            if (transactions != null)
            {
                executableTxSet.Transactions.Count.ShouldBe(transactions.Count);

                foreach (var tx in transactions)
                {
                    executableTxSet.Transactions.ShouldContain(tx);
                }
            }
            else
            {
                executableTxSet.Transactions.Count.ShouldBe(0);
            }
        }

        #endregion
    }
}