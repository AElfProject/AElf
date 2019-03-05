using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.Kernel.Blockchain.Application
{
    public class TransactionResultServiceTests : AElfKernelTestBase
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionResultService _transactionResultService;
        private readonly ILocalEventBus _localEventBus;
        private Block GenesisBlock { get; }

        public TransactionResultServiceTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _transactionResultService = GetRequiredService<ITransactionResultService>();
            _localEventBus = GetRequiredService<ILocalEventBus>();
            GenesisBlock = new Block()
            {
                Header = new BlockHeader()
                {
                    Height = ChainConsts.GenesisBlockHeight,
                    PreviousBlockHash = Hash.Genesis
                },
                Body = new BlockBody()
            };
        }

        private async Task AddTransactionResultsWithPreMiningAsync(Block block, IEnumerable<TransactionResult> results)
        {
            var merkleRoot = block.Header.MerkleTreeRootOfTransactions;
            // Set block to pre mining
            block.Header.MerkleTreeRootOfTransactions = null;

            // // TransactionResults are added during execution
            foreach (var result in results)
            {
                // Add TransactionResult before completing and adding block
                await _transactionResultService.AddTransactionResultAsync(result, block.Header);
            }

            // Set block back to post mining
            block.Header.MerkleTreeRootOfTransactions = merkleRoot;

            // Add block to chain
            var chain = await _blockchainService.GetChainAsync();
            await _blockchainService.AddBlockAsync(block);
            await _blockchainService.AttachBlockToChainAsync(chain, block);
            await _blockchainService.SetBestChainAsync(chain, block.Height, block.GetHash());
        }

        private async Task AddTransactionResultsWithPostMiningAsync(Block block, IEnumerable<TransactionResult> results)
        {
            // Add block to chain
            var chain = await _blockchainService.GetChainAsync();
            await _blockchainService.AddBlockAsync(block);
            await _blockchainService.AttachBlockToChainAsync(chain, block);

            // TransactionResults are added during execution
            foreach (var result in results)
            {
                // Add TransactionResult before completing and adding block
                await _transactionResultService.AddTransactionResultAsync(result, block.Header);
            }

            // Set best chain after execution
            await _blockchainService.SetBestChainAsync(chain, block.Height, block.GetHash());
        }

        private (Block, List<TransactionResult>) GetNextBlockWithTransactionAndResults(BlockHeader previous,
            IEnumerable<Transaction> transactions, ByteString uniqueData = null)
        {
            var block = new Block()
            {
                Header = new BlockHeader()
                {
                    Height = previous.Height + 1,
                    PreviousBlockHash = previous.GetHash(),
                    MerkleTreeRootOfTransactions = Hash.FromString($"block {previous.Height + 1}")
                },
                Body = new BlockBody()
            };
            if (uniqueData != null)
            {
                // piggy back on Bloom to make block unique
                block.Header.Bloom = uniqueData;
            }

            var results = new List<TransactionResult>();
            foreach (var transaction in transactions)
            {
                block.Body.Transactions.Add(transaction.GetHash());
                block.Body.TransactionList.Add(transaction);
                results.Add(new TransactionResult()
                {
                    TransactionId = transaction.GetHash(),
                    RetVal = ByteString.CopyFromUtf8(block.GetHash().ToHex())
                });
            }

            return (block, results);
        }

        private Transaction GetDummyTransactionWithMethodNameAsId(string id)
        {
            return new Transaction()
            {
                From = Address.Zero,
                To = Address.Zero,
                MethodName = id
            };
        }

        [Fact]
        public async Task Add_TransactionResult_With_PreMiningHash()
        {
            await _blockchainService.CreateChainAsync(GenesisBlock);
            var tx = GetDummyTransactionWithMethodNameAsId("tx1");
            var (block, results) = GetNextBlockWithTransactionAndResults(GenesisBlock.Header, new[] {tx});

            var result = results.First();
            await AddTransactionResultsWithPreMiningAsync(block, new[] {result});

            var queried = await _transactionResultService.GetTransactionResultAsync(tx.GetHash());
            queried.ShouldBe(result);
        }

        [Fact]
        public async Task Add_TransactionResult_With_BlockHash()
        {
            await _blockchainService.CreateChainAsync(GenesisBlock);
            var tx = GetDummyTransactionWithMethodNameAsId("tx1");
            var (block, results) = GetNextBlockWithTransactionAndResults(GenesisBlock.Header, new[] {tx});

            var result = results.First();
            // Complete block
            await AddTransactionResultsWithPostMiningAsync(block, new[] {result});

            var queried = await _transactionResultService.GetTransactionResultAsync(tx.GetHash());
            queried.ShouldBe(result);
        }
    }
}