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
    public class TxHubTests : TransactionPoolTestBase
    {
        private readonly TxHub _txHub;
        private readonly IBlockchainService _blockchainService;
        
        public TxHubTests()
        {
            _txHub = GetRequiredService<TxHub>();
            _blockchainService = GetRequiredService<IBlockchainService>();

            AsyncHelper.RunSync(CreateNewChain);
        }
        
        [Fact]
        public async Task Test_TxHub()
        {
            {
                // Empty transaction pool
                // Chain:
                //         BestChainHeight: 1
                // TxHub:
                //         BestChainHeight: 0
                //          AllTransaction: 0
                //   ExecutableTransaction: 0
                ExecutableTransactionShouldBe(Hash.Empty, 0);

                TransactionPoolSizeShouldBe(0);
            }

            var transaction100 = GenerateTransaction(100);
            {
                // Receive a feature transaction twice
                // Chain:
                //         BestChainHeight: 1
                // TxHub:
                //         BestChainHeight: 0
                //          AllTransaction: 1
                //   ExecutableTransaction: 0
                
                // Receive the transaction first time
                await _txHub.HandleTransactionsReceivedAsync(new TransactionsReceivedEvent
                {
                    Transactions = new List<Transaction> {transaction100}
                });

                ExecutableTransactionShouldBe(Hash.Empty, 0);
                
                TransactionPoolSizeShouldBe(1);
                TransactionShouldInPool(transaction100);
                
                // Receive the same transaction again
                await _txHub.HandleTransactionsReceivedAsync(new TransactionsReceivedEvent
                {
                    Transactions = new List<Transaction> {transaction100}
                });

                ExecutableTransactionShouldBe(Hash.Empty, 0);

                TransactionPoolSizeShouldBe(1);
                TransactionShouldInPool(transaction100);
            }

            {
                // Receive a valid transaction 
                // Chain:
                //         BestChainHeight: 1
                // TxHub:
                //         BestChainHeight: 0
                //          AllTransaction: 2
                //   ExecutableTransaction: 0
                var chain = await _blockchainService.GetChainAsync();
                var transactionValid = GenerateTransaction(chain.BestChainHeight, chain.BestChainHash);

                await _txHub.HandleTransactionsReceivedAsync(new TransactionsReceivedEvent
                {
                    Transactions = new List<Transaction> {transactionValid}
                });

                TransactionPoolSizeShouldBe(2);
                TransactionShouldInPool(transaction100);
                TransactionShouldInPool(transactionValid);

                // Receive a block
                // Chain:
                //         BestChainHeight: 2
                // TxHub:
                //         BestChainHeight: 0
                //          AllTransaction: 1
                //   ExecutableTransaction: 0
                var transactionNotInPool = GenerateTransaction(chain.BestChainHeight, chain.BestChainHash);

                var newBlock = await AddBlock(new List<Transaction>
                {
                    transactionValid,
                    transactionNotInPool
                });

                await _blockchainService.AddBlockAsync(newBlock);
                await _blockchainService.AttachBlockToChainAsync(chain, newBlock);
                await _blockchainService.SetBestChainAsync(chain, newBlock.Height, newBlock.GetHash());

                await _txHub.HandleBlockAcceptedAsync(new BlockAcceptedEvent
                {
                    BlockHeader = newBlock.Header
                });

                TransactionPoolSizeShouldBe(1);
                TransactionShouldInPool(transaction100);
            }

            {
                // Receive best chain found event
                // Chain:
                //         BestChainHeight: 2
                // TxHub:
                //         BestChainHeight: 2
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
                TransactionShouldInPool(transaction100);
            }
            
            {
                // Receive a valid transaction and a invalid transaction
                // Chain:
                //         BestChainHeight: 2
                // TxHub:
                //         BestChainHeight: 2
                //          AllTransaction: 3
                //   ExecutableTransaction: 1
                var chain = await _blockchainService.GetChainAsync();
                var transactionValid = GenerateTransaction(chain.BestChainHeight, chain.BestChainHash);
                var transactionInvalid = GenerateTransaction(chain.BestChainHeight - 1);

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
                TransactionShouldInPool(transaction100);
                TransactionShouldInPool(transactionValid);
                TransactionShouldInPool(transactionInvalid);

                // Receive lib found event
                // Chain:
                //         BestChainHeight: 2
                // TxHub:
                //         BestChainHeight: 2
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
                TransactionShouldInPool(transaction100);
                TransactionShouldInPool(transactionValid);
                TransactionShouldInPool(transactionInvalid);
            }

            {
                // After 65 blocks
                // Chain:
                //         BestChainHeight: 67
                // TxHub:
                //         BestChainHeight: 67
                //          AllTransaction: 1
                //   ExecutableTransaction: 0
                var chain = await _blockchainService.GetChainAsync();
                var bestChainHeight = chain.BestChainHeight;
                for (var i = 0; i < ChainConsts.ReferenceBlockValidPeriod + 1; i++)
                {
                    var transaction = GenerateTransaction(bestChainHeight + i);
                    await AddBlock(new List<Transaction> {transaction});
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
                TransactionShouldInPool(transaction100);
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

        #region private methods

        private async Task<Chain> CreateNewChain()
        {
            var genesisBlock = new Block
            {
                Header = new BlockHeader
                {
                    Height = ChainConsts.GenesisBlockHeight,
                    PreviousBlockHash = Hash.Empty
                },
                Body = new BlockBody()
            };
            var chain = await _blockchainService.CreateChainAsync(genesisBlock);
            return chain;
        }

        private async Task<Block> AddBlock(List<Transaction> transactions)
        {
            var chain = await _blockchainService.GetChainAsync();
            var newBlock = new Block
            {
                Header = new BlockHeader
                {
                    Height = chain.BestChainHeight + 1,
                    PreviousBlockHash = chain.BestChainHash,
                    Time = Timestamp.FromDateTime(DateTime.UtcNow)
                },
                Body = new BlockBody()
            };
            foreach (var tx in transactions)
            {
                newBlock.Body.AddTransaction(tx);
            }

            await _blockchainService.AddBlockAsync(newBlock);
            await _blockchainService.AttachBlockToChainAsync(chain, newBlock);
            await _blockchainService.SetBestChainAsync(chain, newBlock.Height, newBlock.GetHash());

            return newBlock;
        }

        private Transaction GenerateTransaction(long refBlockNumber, Hash refBlockHash = null)
        {
            var transaction = new Transaction
            {
                From = Address.Zero,
                To = Address.Zero,
                MethodName = Guid.NewGuid().ToString(),
                RefBlockNumber = refBlockNumber,
                RefBlockPrefix = refBlockHash == null
                    ? ByteString.Empty
                    : ByteString.CopyFrom(refBlockHash.DumpByteArray().Take(4).ToArray())
            };

            return transaction;
        }

        #endregion
    }
}