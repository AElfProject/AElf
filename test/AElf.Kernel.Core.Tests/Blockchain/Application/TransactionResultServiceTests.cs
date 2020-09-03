using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.Kernel.Blockchain.Application
{
    public class TransactionResultServiceTests : AElfKernelWithChainTestBase
    {
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionResultService _transactionResultService;
        private readonly ITransactionResultManager _transactionResultManager;
        private readonly ITransactionBlockIndexManager _transacionBlockIndexManager;
        private readonly ILocalEventBus _localEventBus;
        private readonly ITransactionBlockIndexService _transactionBlockIndexService;

        public TransactionResultServiceTests()
        {
            _transactionBlockIndexService = GetRequiredService<ITransactionBlockIndexService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _transactionResultService = GetRequiredService<ITransactionResultService>();
            _transactionResultManager = GetRequiredService<ITransactionResultManager>();
            _transacionBlockIndexManager = GetRequiredService<ITransactionBlockIndexManager>();
            _localEventBus = GetRequiredService<ILocalEventBus>();
        }

        private async Task AddTransactionResultsWithPostMiningAsync(Block block, IEnumerable<TransactionResult> results)
        {
            // Add block to chain
            var chain = await _blockchainService.GetChainAsync();
            await _blockchainService.AddBlockAsync(block);
            await _blockchainService.AttachBlockToChainAsync(chain, block);

            // TransactionResults are added during execution
            // Add TransactionResult before completing and adding block
            await _transactionResultService.AddTransactionResultsAsync(results.ToList(), block.Header);

            // Set best chain after execution
            await _blockchainService.SetBestChainAsync(chain, block.Height, block.GetHash());

            var blockIndex = new BlockIndex
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            };

            await _transactionBlockIndexService.AddBlockIndexAsync(block.Body.TransactionIds, blockIndex);
        }

        private (Block, List<TransactionResult>) GetNextBlockWithTransactionAndResults(BlockHeader previous,
            IEnumerable<Transaction> transactions, ByteString uniqueData = null)
        {
            var block = _kernelTestHelper.GenerateBlock(previous.Height, previous.GetHash());
            if (uniqueData != null)
            {
                // piggy back on Bloom to make block unique
                block.Header.Bloom = uniqueData;
            }

            var results = new List<TransactionResult>();
            foreach (var transaction in transactions)
            {
                block.Body.TransactionIds.Add(transaction.GetHash());
                results.Add(new TransactionResult()
                {
                    TransactionId = transaction.GetHash(),
                    ReturnValue = ByteString.CopyFromUtf8(block.GetHash().ToHex())
                });
            }

            return (block, results);
        }

        [Fact]
        public async Task Add_TransactionResult_With_BlockHash()
        {
            var tx = _kernelTestHelper.GenerateTransaction();
            var (block, results) = GetNextBlockWithTransactionAndResults(_kernelTestHelper.BestBranchBlockList.Last()
                .Header, new[] {tx});

            var result = results.First();
            
            (await _transactionResultService.GetTransactionResultAsync(tx.GetHash())).ShouldBeNull();
            // Complete block
            await AddTransactionResultsWithPostMiningAsync(block, new[] {result});

            var queried = await _transactionResultService.GetTransactionResultAsync(tx.GetHash());
            queried.ShouldBe(result);

            var queried2 = await _transactionResultService.GetTransactionResultAsync(tx.GetHash(), block.GetHash());
            queried2.ShouldBe(result);
        }

        [Fact]
        public async Task Query_TransactionResult_On_BestChain()
        {
            var tx = _kernelTestHelper.GenerateTransaction();
            var (block11, results11) =
                GetNextBlockWithTransactionAndResults(_kernelTestHelper.BestBranchBlockList.Last().Header, new[] {tx},
                    ByteString.CopyFromUtf8("branch_1"));
            var (block21, results21) =
                GetNextBlockWithTransactionAndResults(_kernelTestHelper.BestBranchBlockList.Last().Header, new[] {tx},
                    ByteString.CopyFromUtf8("branch_2"));

            // Add branch 1
            await AddTransactionResultsWithPostMiningAsync(block11, new[] {results11.First()});

            // Add branch 2
            await AddTransactionResultsWithPostMiningAsync(block21, new[] {results21.First()});

            Assert.NotEqual(results11.First(), results21.First());

            var chain = await _blockchainService.GetChainAsync();

            // Set BestChain to branch 1
            await _blockchainService.SetBestChainAsync(chain, block11.Height, block11.Header.GetHash());
            var queried = await _transactionResultService.GetTransactionResultAsync(tx.GetHash());
            queried.ShouldBe(results11.First());

            var queried2 = await _transactionResultService.GetTransactionResultAsync(tx.GetHash(), block11.GetHash());
            queried2.ShouldBe(results11.First());

            // Set BestChain to branch 2
            await _blockchainService.SetBestChainAsync(chain, block21.Height, block21.Header.GetHash());
            queried = await _transactionResultService.GetTransactionResultAsync(tx.GetHash());
            queried.ShouldBe(results21.First());

            queried2 = await _transactionResultService.GetTransactionResultAsync(tx.GetHash(), block21.GetHash());
            queried2.ShouldBe(results21.First());
        }

        [Fact]
        public async Task Query_TransactionResult_After_Execution()
        {
            var tx1 = _kernelTestHelper.GenerateTransaction();
            var (block11, results11) =
                GetNextBlockWithTransactionAndResults(_kernelTestHelper.BestBranchBlockList.Last().Header, new[] {tx1});

            var tx2 = _kernelTestHelper.GenerateTransaction();
            var (block12, results12) =
                GetNextBlockWithTransactionAndResults(block11.Header, new[] {tx2});

            // Add block 1
            await AddTransactionResultsWithPostMiningAsync(block11, new[] {results11.First()});

            // Add block 2
            await AddTransactionResultsWithPostMiningAsync(block12, new[] {results12.First()});


            #region Before LIB

            // Before LIB, transaction result is saved with PreMiningHash but not with PostMiningHash (normal BlockHash)
            {
                var queried = await _transactionResultService.GetTransactionResultAsync(tx2.GetHash());
                queried.ShouldBe(results12.First());

                var queried2 =
                    await _transactionResultService.GetTransactionResultAsync(tx2.GetHash(), block12.GetHash());
                queried2.ShouldBe(results12.First());

                var queried3 =
                    await _transactionResultService.GetTransactionResultAsync(tx2.GetHash(), block12.GetHash());
                queried3.ShouldBe(results12.First());
                // PreMiningHash
                // var resultWithPreMiningHash =
                //     await _transactionResultManager.GetTransactionResultAsync(tx1.GetHash(),
                //         block11.Header.GetDisambiguatingHash());
                // resultWithPreMiningHash.ShouldBe(results11.First());

                // PostMiningHash
                var resultWithPostMiningHash =
                    await _transactionResultManager.GetTransactionResultAsync(tx1.GetHash(),
                        block11.Header.GetHash());
                //resultWithPostMiningHash.ShouldBeNull();
            }

            await _transactionResultService.ProcessTransactionResultAfterExecutionAsync(block11.Header,
                block11.Body.TransactionIds.ToList());

            // After LIB, transaction result is re-saved with PostMiningHash (normal BlockHash)
            {
                var queried = await _transactionResultService.GetTransactionResultAsync(tx2.GetHash());
                queried.ShouldBe(results12.First());

                var queried2 =
                    await _transactionResultService.GetTransactionResultAsync(tx2.GetHash(), block12.GetHash());
                queried2.ShouldBe(results12.First());
                // PreMiningHash
                // var resultWithPreMiningHash =
                //     await _transactionResultManager.GetTransactionResultAsync(tx1.GetHash(),
                //         block11.Header.GetDisambiguatingHash());
                // resultWithPreMiningHash.ShouldBeNull();

                // PostMiningHash
                var resultWithPostMiningHash =
                    await _transactionResultManager.GetTransactionResultAsync(tx1.GetHash(),
                        block11.Header.GetHash());
                resultWithPostMiningHash.ShouldBe(results11.First());
            }

            #endregion
        }

        [Fact]
        public async Task Query_TransactionResult_With_Index()
        {
            var tx = _kernelTestHelper.GenerateTransaction();
            var (block, results) =
                GetNextBlockWithTransactionAndResults(_kernelTestHelper.BestBranchBlockList.Last().Header, new[] {tx});

            var result = results.First();
            // Complete block
            await AddTransactionResultsWithPostMiningAsync(block, new[] {result});

            var transactionBlockIndex = new TransactionBlockIndex()
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            };
            await _transacionBlockIndexManager.SetTransactionBlockIndexAsync(tx.GetHash(), transactionBlockIndex);

            var queried = await _transactionResultService.GetTransactionResultAsync(tx.GetHash());
            queried.ShouldBe(result);

            var queried2 = await _transactionResultService.GetTransactionResultAsync(tx.GetHash(), block.GetHash());
            queried2.ShouldBe(result);
        }
    }
}