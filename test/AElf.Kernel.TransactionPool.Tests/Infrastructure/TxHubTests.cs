using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public class TxHubTests : TransactionPoolWithChainTestBase
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
                // Receive a feature transaction twice
                // Chain:
                //         BestChainHeight: 11
                // TxHub:
                //         BestChainHeight: 0
                //          AllTransaction: 1
                //   ExecutableTransaction: 0
                
                // Receive the transaction first time
                await _txHub.HandleTransactionsReceivedAsync(new TransactionsReceivedEvent
                {
                    Transactions = new List<Transaction> {transactionHeight100}
                });

                ExecutableTransactionShouldBe(Hash.Empty, 0);
                
                TransactionPoolSizeShouldBe(1);
                TransactionShouldInPool(transactionHeight100);
                
                // Receive the same transaction again
                await _txHub.HandleTransactionsReceivedAsync(new TransactionsReceivedEvent
                {
                    Transactions = new List<Transaction> {transactionHeight100}
                });

                ExecutableTransactionShouldBe(Hash.Empty, 0);

                TransactionPoolSizeShouldBe(1);
                TransactionShouldInPool(transactionHeight100);
            }

            {
                // Receive a valid transaction 
                // Chain:
                //         BestChainHeight: 11
                // TxHub:
                //         BestChainHeight: 0
                //          AllTransaction: 2
                //   ExecutableTransaction: 0
                var chain = await _blockchainService.GetChainAsync();
                var transactionValid = _kernelTestHelper.GenerateTransaction(chain.BestChainHeight, chain.BestChainHash);

                await _txHub.HandleTransactionsReceivedAsync(new TransactionsReceivedEvent
                {
                    Transactions = new List<Transaction> {transactionValid}
                });

                TransactionPoolSizeShouldBe(2);
                TransactionShouldInPool(transactionHeight100);
                TransactionShouldInPool(transactionValid);

                // Receive a block
                // Chain:
                //         BestChainHeight: 12
                // TxHub:
                //         BestChainHeight: 0
                //          AllTransaction: 1
                //   ExecutableTransaction: 0
                var transactionNotInPool = _kernelTestHelper.GenerateTransaction(chain.BestChainHeight, chain.BestChainHash);

                var newBlock = await _kernelTestHelper.AttachBlockToBestChain(new List<Transaction>
                {
                    transactionValid,
                    transactionNotInPool
                });

                await _txHub.HandleBlockAcceptedAsync(new BlockAcceptedEvent
                {
                    BlockHeader = newBlock.Header
                });

                TransactionPoolSizeShouldBe(1);
                TransactionShouldInPool(transactionHeight100);
            }

            {
                // Receive best chain found event
                // Chain:
                //         BestChainHeight: 12
                // TxHub:
                //         BestChainHeight: 12
                //          AllTransaction: 1
                //   ExecutableTransaction: 0
                var chain = await _blockchainService.GetChainAsync();

                await _txHub.HandleBestChainFoundAsync(new BestChainFoundEventData
                {
                    BlockHash = chain.BestChainHash,
                    BlockHeight = chain.BestChainHeight
                });
                
                ExecutableTransactionShouldBe(chain.BestChainHash, chain.BestChainHeight);
                
                TransactionPoolSizeShouldBe(1);
                TransactionShouldInPool(transactionHeight100);
            }
            
            {
                // Receive a valid transaction and a invalid transaction
                // Chain:
                //         BestChainHeight: 12
                // TxHub:
                //         BestChainHeight: 12
                //          AllTransaction: 3
                //   ExecutableTransaction: 1
                var chain = await _blockchainService.GetChainAsync();
                var transactionValid = _kernelTestHelper.GenerateTransaction(chain.BestChainHeight, chain.BestChainHash);
                var transactionInvalid = _kernelTestHelper.GenerateTransaction(chain.BestChainHeight - 1);

                await _txHub.HandleTransactionsReceivedAsync(new TransactionsReceivedEvent
                {
                    Transactions = new List<Transaction>
                    {
                        transactionValid, 
                        transactionInvalid
                    }
                });
                
                ExecutableTransactionShouldBe(chain.BestChainHash, chain.BestChainHeight, new List<Transaction>
                {
                    transactionValid
                });
                
                TransactionPoolSizeShouldBe(3);
                TransactionShouldInPool(transactionHeight100);
                TransactionShouldInPool(transactionValid);
                TransactionShouldInPool(transactionInvalid);

                // Receive lib found event
                // Chain:
                //         BestChainHeight: 12
                // TxHub:
                //         BestChainHeight: 12
                //          AllTransaction: 3
                //   ExecutableTransaction: 1
                await _txHub.HandleNewIrreversibleBlockFoundAsync(new NewIrreversibleBlockFoundEvent
                {
                    BlockHash = chain.BestChainHash,
                    BlockHeight = chain.BestChainHeight
                });

                ExecutableTransactionShouldBe(chain.BestChainHash, chain.BestChainHeight, new List<Transaction>
                {
                    transactionValid
                });
                
                TransactionPoolSizeShouldBe(3);
                TransactionShouldInPool(transactionHeight100);
                TransactionShouldInPool(transactionValid);
                TransactionShouldInPool(transactionInvalid);
            }

            {
                // After 65 blocks
                // Chain:
                //         BestChainHeight: 78
                // TxHub:
                //         BestChainHeight: 78
                //          AllTransaction: 1
                //   ExecutableTransaction: 0
                var chain = await _blockchainService.GetChainAsync();
                var bestChainHeight = chain.BestChainHeight;
                for (var i = 0; i < KernelConstants.ReferenceBlockValidPeriod + 1; i++)
                {
                    var transaction = _kernelTestHelper.GenerateTransaction(bestChainHeight + i);
                    await _kernelTestHelper.AttachBlockToBestChain(new List<Transaction> {transaction});
                    chain = await _blockchainService.GetChainAsync();
                    await _txHub.HandleBestChainFoundAsync(new BestChainFoundEventData
                    {
                        BlockHash = chain.BestChainHash,
                        BlockHeight = chain.BestChainHeight
                    });
                    await _txHub.HandleNewIrreversibleBlockFoundAsync(new NewIrreversibleBlockFoundEvent
                    {
                        BlockHash = chain.BestChainHash,
                        BlockHeight = chain.BestChainHeight
                    });
                }

                ExecutableTransactionShouldBe(chain.BestChainHash, chain.BestChainHeight);
                
                TransactionPoolSizeShouldBe(1);
                TransactionShouldInPool(transactionHeight100);
            }
        }

        #region check methods

        private void TransactionShouldInPool(Transaction transaction)
        {
            var existTransactionReceipt = _txHub.GetTransactionReceiptAsync(transaction.GetHash()).Result;
            existTransactionReceipt.Transaction.ShouldBe(transaction);
        }

        private void TransactionPoolSizeShouldBe(int size)
        {
            var transactionPoolSize = _txHub.GetTransactionPoolSizeAsync().Result;
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