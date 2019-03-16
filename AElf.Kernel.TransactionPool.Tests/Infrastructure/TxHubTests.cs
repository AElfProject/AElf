using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.TransactionPool.Infrastructure;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Kernel.TransactionPool.Tests.Infrastructure
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
        
        private Transaction GenerateTransaction(long refBlockNumber, Hash refBlockHash = null)
        {
            var transaction = new Transaction
            {
                From = Address.Zero,
                To = Address.Zero,
                MethodName = Guid.NewGuid().ToString(),
                RefBlockNumber = refBlockNumber,
                RefBlockPrefix = refBlockHash == null
                    ? null
                    : ByteString.CopyFrom(refBlockHash.DumpByteArray().Take(4).ToArray())
            };

            return transaction;
        }

        [Fact]
        public async Task Get_ExecutableTransactionSet_ReturnEmpty()
        {
            // Empty transaction pool
            {
                // Executable transaction is empty 
                var executableTxSet = await _txHub.GetExecutableTransactionSetAsync();
                executableTxSet.PreviousBlockHash.ShouldBe(Hash.Empty);
                executableTxSet.PreviousBlockHeight.ShouldBe(0);
                executableTxSet.Transactions.Count.ShouldBe(0);
                
                var transactionPoolSize = await _txHub.GetTransactionPoolSizeAsync();
                transactionPoolSize.ShouldBe(0);
            }

            // Receive a feature transaction
            {
                var newTransaction = GenerateTransaction(100);

                // Receive the transaction first time
                await _txHub.HandleTransactionsReceivedAsync(new TransactionsReceivedEvent
                {
                    Transactions = new List<Transaction> {newTransaction}
                });

                var executableTxSet = await _txHub.GetExecutableTransactionSetAsync();
                executableTxSet.PreviousBlockHash.ShouldBe(Hash.Empty);
                executableTxSet.PreviousBlockHeight.ShouldBe(0);
                executableTxSet.Transactions.Count.ShouldBe(0);

                var transactionPoolSize = await _txHub.GetTransactionPoolSizeAsync();
                transactionPoolSize.ShouldBe(1);
                
                // Receive the same transaction again
                await _txHub.HandleTransactionsReceivedAsync(new TransactionsReceivedEvent
                {
                    Transactions = new List<Transaction> {newTransaction}
                });

                executableTxSet = await _txHub.GetExecutableTransactionSetAsync();
                executableTxSet.PreviousBlockHash.ShouldBe(Hash.Empty);
                executableTxSet.PreviousBlockHeight.ShouldBe(0);
                executableTxSet.Transactions.Count.ShouldBe(0);

                transactionPoolSize = await _txHub.GetTransactionPoolSizeAsync();
                transactionPoolSize.ShouldBe(1);

                var existTransactionReceipt = await _txHub.GetTransactionReceiptAsync(newTransaction.GetHash());
                existTransactionReceipt.Transaction.ShouldBe(newTransaction);
            }
            
            // Receive a block
            {
                var chain = await _blockchainService.GetChainAsync();
                var transaction1 = GenerateTransaction(chain.BestChainHeight + 1);
                var transaction2 = GenerateTransaction(chain.BestChainHeight + 1);
                
                await _txHub.HandleTransactionsReceivedAsync(new TransactionsReceivedEvent
                {
                    Transactions = new List<Transaction> {transaction1}
                });
                
                var transactionPoolSize = await _txHub.GetTransactionPoolSizeAsync();
                transactionPoolSize.ShouldBe(2);
                
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
                newBlock.Body.AddTransaction(transaction1);
                newBlock.Body.AddTransaction(transaction2);
                
                await _blockchainService.AddBlockAsync(newBlock);
                await _blockchainService.AttachBlockToChainAsync(chain, newBlock);

                await _txHub.HandleBlockAcceptedAsync(new BlockAcceptedEvent
                {
                    BlockHeader = newBlock.Header
                });
                
                transactionPoolSize = await _txHub.GetTransactionPoolSizeAsync();
                transactionPoolSize.ShouldBe(1);
            }

            // Receive best chain found event
            {
                var chain = await _blockchainService.GetChainAsync();
                var newBlock = new Block
                {
                    Header = new BlockHeader
                    {
                        Height = chain.LongestChainHeight + 1,
                        PreviousBlockHash = chain.LongestChainHash,
                        Time = Timestamp.FromDateTime(DateTime.UtcNow)
                    },
                    Body = new BlockBody()
                };
                newBlock.Body.AddTransaction(GenerateTransaction(newBlock.Height));
                newBlock.Body.AddTransaction(GenerateTransaction(newBlock.Height));
                
                await _blockchainService.AddBlockAsync(newBlock);
                await _blockchainService.AttachBlockToChainAsync(chain, newBlock);
                await _blockchainService.SetBestChainAsync(chain, newBlock.Height, newBlock.GetHash());

                await _txHub.HandleBestChainFoundAsync(new BestChainFoundEventData
                {
                    BlockHash = newBlock.GetHash(),
                    BlockHeight = newBlock.Height
                });
                
                var executableTxSet = await _txHub.GetExecutableTransactionSetAsync();
                executableTxSet.PreviousBlockHash.ShouldBe(newBlock.GetHash());
                executableTxSet.PreviousBlockHeight.ShouldBe(newBlock.Height);
                executableTxSet.Transactions.Count.ShouldBe(0);
                
                var transactionPoolSize = await _txHub.GetTransactionPoolSizeAsync();
                transactionPoolSize.ShouldBe(1);
            }
            
            // Receive lib found event
            {
                var chain = await _blockchainService.GetChainAsync();
                var transaction1 = GenerateTransaction(chain.BestChainHeight);
                var transaction2 = GenerateTransaction(chain.BestChainHeight - 1);

                await _txHub.HandleTransactionsReceivedAsync(new TransactionsReceivedEvent
                {
                    Transactions = new List<Transaction>
                    {
                        transaction1, 
                        transaction2
                    }
                });
                
                var executableTxSet = await _txHub.GetExecutableTransactionSetAsync();
                executableTxSet.PreviousBlockHash.ShouldBe(chain.BestChainHash);
                executableTxSet.PreviousBlockHeight.ShouldBe(chain.BestChainHeight);
                executableTxSet.Transactions.Count.ShouldBe(1);
                
                var transactionPoolSize = await _txHub.GetTransactionPoolSizeAsync();
                transactionPoolSize.ShouldBe(3);

                await _txHub.HandleNewIrreversibleBlockFoundAsync(new NewIrreversibleBlockFoundEvent
                {
                    BlockHash = chain.BestChainHash,
                    BlockHeight = chain.BestChainHeight
                });
                
                executableTxSet = await _txHub.GetExecutableTransactionSetAsync();
                executableTxSet.PreviousBlockHash.ShouldBe(chain.BestChainHash);
                executableTxSet.PreviousBlockHeight.ShouldBe(chain.BestChainHeight);
                executableTxSet.Transactions.Count.ShouldBe(1);
                
                transactionPoolSize = await _txHub.GetTransactionPoolSizeAsync();
                transactionPoolSize.ShouldBe(2);
            }

        }



    }
}