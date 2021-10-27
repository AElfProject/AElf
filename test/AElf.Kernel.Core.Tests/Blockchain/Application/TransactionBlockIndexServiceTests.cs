using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Blockchain.Application
{
    public class TransactionBlockIndexServiceTests : AElfKernelWithChainTestBase
    {
        private readonly ITransactionBlockIndexService _transactionBlockIndexService;
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionBlockIndexManager _transactionBlockIndexManager;
        private readonly ITransactionBlockIndexProvider _transactionBlockIndexProvider;

        public TransactionBlockIndexServiceTests()
        {
            _transactionBlockIndexService = GetRequiredService<ITransactionBlockIndexService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _transactionBlockIndexManager = GetRequiredService<ITransactionBlockIndexManager>();
            _transactionBlockIndexProvider = GetRequiredService<ITransactionBlockIndexProvider>();
        }

        [Fact]
        public async Task UpdateOneBlockIndexWithoutBestChain()
        {
            var previousBlockHeader = _kernelTestHelper.BestBranchBlockList.Last().Header;
            var block = _kernelTestHelper.GenerateBlock(previousBlockHeader.Height, HashHelper.ComputeFrom("PreBlockHash"));
            var chain = await _blockchainService.GetChainAsync();
            await AddBlockAsync(chain, block);

            var txId = HashHelper.ComputeFrom("Transaction");
            var blockIndex = new BlockIndex
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            };
            await _transactionBlockIndexService.AddBlockIndexAsync(new List<Hash> {txId}, blockIndex);

            var actual = await _transactionBlockIndexService.GetTransactionBlockIndexAsync(txId);
            Assert.Null(actual);

            var cacheInBestBranch = await _transactionBlockIndexService.GetTransactionBlockIndexAsync(txId);
            Assert.Null(cacheInBestBranch);

            var cacheInForkBranch =
                await _transactionBlockIndexService.ValidateTransactionBlockIndexExistsInBranchAsync(txId,
                    block.GetHash());
            Assert.True(cacheInForkBranch);
        }

        [Fact]
        public async Task UpdateOneBlockIndexWithBestChain()
        {
            var previousBlockHeader = _kernelTestHelper.BestBranchBlockList.Last().Header;
            var block = _kernelTestHelper.GenerateBlock(previousBlockHeader.Height, HashHelper.ComputeFrom("PreBlockHash"));
            var txId = HashHelper.ComputeFrom("Transaction");
            var blockIndex = new BlockIndex
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            };
            await _transactionBlockIndexService.AddBlockIndexAsync(new List<Hash> {txId}, blockIndex);
            var chain = await _blockchainService.GetChainAsync();
            await AddBlockAsync(chain, block);
            await SetBestChain(chain, block);

            var actual = await _transactionBlockIndexService.GetTransactionBlockIndexAsync(txId);
            Assert.Equal(blockIndex, actual);

            var cacheInBestBranch = await _transactionBlockIndexService.GetTransactionBlockIndexAsync(txId);
            Assert.Equal(blockIndex, cacheInBestBranch);
        }

        [Fact]
        public async Task UpdateTwoBlockIndexesWithoutBestChain()
        {
            var previousBlockHeader = _kernelTestHelper.BestBranchBlockList.Last().Header;
            var block1 = _kernelTestHelper.GenerateBlock(previousBlockHeader.Height, HashHelper.ComputeFrom("PreBlockHash1"));
            var block2 = _kernelTestHelper.GenerateBlock(previousBlockHeader.Height, HashHelper.ComputeFrom("PreBlockHash2"));

            var chain = await _blockchainService.GetChainAsync();
            await AddBlockAsync(chain, block1);

            var txId = HashHelper.ComputeFrom("Transaction");
            var blockIndex1 = new BlockIndex
            {
                BlockHash = block1.GetHash(),
                BlockHeight = block1.Height
            };
            await _transactionBlockIndexService.AddBlockIndexAsync(new List<Hash> {txId}, blockIndex1);

            chain = await _blockchainService.GetChainAsync();
            await AddBlockAsync(chain, block2);

            var blockIndex2 = new BlockIndex
            {
                BlockHash = block2.GetHash(),
                BlockHeight = block2.Height
            };
            await _transactionBlockIndexService.AddBlockIndexAsync(new List<Hash> {txId}, blockIndex2);

            var actual = await _transactionBlockIndexService.GetTransactionBlockIndexAsync(txId);
            Assert.Null(actual);

            var cacheInBestBranch = await _transactionBlockIndexService.GetTransactionBlockIndexAsync(txId);
            Assert.Null(cacheInBestBranch);

            var cacheBlockIndex1 =
                await _transactionBlockIndexService.ValidateTransactionBlockIndexExistsInBranchAsync(txId,
                    block1.GetHash());
            Assert.True(cacheBlockIndex1);

            var cacheBlockIndex2 =
                await _transactionBlockIndexService.ValidateTransactionBlockIndexExistsInBranchAsync(txId,
                    block2.GetHash());
            Assert.True(cacheBlockIndex2);
        }

        [Fact]
        public async Task UpdateTwoBlockIndexesWithFirstBestChain()
        {
            var previousBlockHeader = _kernelTestHelper.BestBranchBlockList.Last().Header;
            var block1 = _kernelTestHelper.GenerateBlock(previousBlockHeader.Height, HashHelper.ComputeFrom("PreBlockHash1"));
            var block2 = _kernelTestHelper.GenerateBlock(previousBlockHeader.Height, HashHelper.ComputeFrom("PreBlockHash2"));

            var txId = HashHelper.ComputeFrom("Transaction");

            var chain = await _blockchainService.GetChainAsync();
            await AddBlockAsync(chain, block1);
            await SetBestChain(chain, block1);

            var blockIndex1 = new BlockIndex
            {
                BlockHash = block1.GetHash(),
                BlockHeight = block1.Height
            };
            await _transactionBlockIndexService.AddBlockIndexAsync(new List<Hash> {txId}, blockIndex1);

            var blockIndex2 = new BlockIndex
            {
                BlockHash = block2.GetHash(),
                BlockHeight = block2.Height
            };
            await _transactionBlockIndexService.AddBlockIndexAsync(new List<Hash> {txId}, blockIndex2);

            chain = await _blockchainService.GetChainAsync();
            await AddBlockAsync(chain, block2);

            var actual = await _transactionBlockIndexService.GetTransactionBlockIndexAsync(txId);
            Assert.Equal(blockIndex1, actual);

            var cacheBlockIndex1 =
                await _transactionBlockIndexService.ValidateTransactionBlockIndexExistsInBranchAsync(txId,
                    chain.BestChainHash);
            Assert.True(cacheBlockIndex1);

            var cacheBlockIndex2 =
                await _transactionBlockIndexService.ValidateTransactionBlockIndexExistsInBranchAsync(txId,
                    block2.GetHash());
            Assert.True(cacheBlockIndex2);
        }

        [Fact]
        public async Task UpdateTwoBlockIndexesWithSecondBestChain()
        {
            var previousBlockHeader = _kernelTestHelper.BestBranchBlockList.Last().Header;
            var block1 = _kernelTestHelper.GenerateBlock(previousBlockHeader.Height, HashHelper.ComputeFrom("PreBlockHash1"));
            var block2 = _kernelTestHelper.GenerateBlock(previousBlockHeader.Height, HashHelper.ComputeFrom("PreBlockHash2"));

            var txId = HashHelper.ComputeFrom("Transaction");
            var chain = await _blockchainService.GetChainAsync();
            await AddBlockAsync(chain, block1);

            var blockIndex1 = new BlockIndex
            {
                BlockHash = block1.GetHash(),
                BlockHeight = block1.Height
            };
            await _transactionBlockIndexService.AddBlockIndexAsync(new List<Hash> {txId}, blockIndex1);

            var blockIndex2 = new BlockIndex
            {
                BlockHash = block2.GetHash(),
                BlockHeight = block2.Height
            };
            await _transactionBlockIndexService.AddBlockIndexAsync(new List<Hash> {txId}, blockIndex2);

            chain = await _blockchainService.GetChainAsync();
            await AddBlockAsync(chain, block2);
            await SetBestChain(chain, block2);

            var actual = await _transactionBlockIndexService.GetTransactionBlockIndexAsync(txId);
            Assert.Equal(blockIndex2, actual);

            var cacheBlockIndex1InBestChain =
                await _transactionBlockIndexService.ValidateTransactionBlockIndexExistsInBranchAsync(txId,
                    chain.BestChainHash);
            Assert.True(cacheBlockIndex1InBestChain);

            var cacheBlockIndex1 =
                await _transactionBlockIndexService.ValidateTransactionBlockIndexExistsInBranchAsync(txId,
                    block1.GetHash());
            Assert.True(cacheBlockIndex1);
        }

        [Fact]
        public async Task GetTransactionBlockIndexBelowLibTest()
        {
            var previousBlockHeader = _kernelTestHelper.BestBranchBlockList.Last().Header;
            var block1 =
                _kernelTestHelper.GenerateBlock(previousBlockHeader.Height, HashHelper.ComputeFrom("PreBlockHash1"));
            var block2 =
                _kernelTestHelper.GenerateBlock(previousBlockHeader.Height, previousBlockHeader.PreviousBlockHash);

            var txId = HashHelper.ComputeFrom("Transaction");
            var chain = await _blockchainService.GetChainAsync();
            await AddBlockAsync(chain, block1);

            var blockIndex =
                await _transactionBlockIndexService.GetTransactionBlockIndexAsync(txId);
            blockIndex.ShouldBeNull();

            var blockIndex1 = new BlockIndex
            {
                BlockHash = block1.GetHash(),
                BlockHeight = block1.Height
            };
            await _transactionBlockIndexService.AddBlockIndexAsync(new List<Hash> {txId}, blockIndex1);

            var blockIndex2 = new BlockIndex
            {
                BlockHash = block2.GetHash(),
                BlockHeight = block2.Height
            };
            await _transactionBlockIndexService.AddBlockIndexAsync(new List<Hash> {txId}, blockIndex2);

            chain = await _blockchainService.GetChainAsync();
            await AddBlockAsync(chain, block2);
            await SetBestChain(chain, block2);

            {
                var existsOnForkBranch =
                    await _transactionBlockIndexService.ValidateTransactionBlockIndexExistsInBranchAsync(txId,
                        block1.GetHash());
                Assert.True(existsOnForkBranch);

                var actualBlockIndexOnBestChain =
                    await _transactionBlockIndexService.GetTransactionBlockIndexAsync(txId);
                Assert.Equal(blockIndex2, actualBlockIndexOnBestChain);

                var transactionBlockIndex = await _transactionBlockIndexManager.GetTransactionBlockIndexAsync(txId);
                transactionBlockIndex.BlockHash.ShouldBe(blockIndex2.BlockHash);
                transactionBlockIndex.BlockHeight.ShouldBe(blockIndex2.BlockHeight);
                transactionBlockIndex.PreviousExecutionBlockIndexList.Count.ShouldBe(1);
                transactionBlockIndex.PreviousExecutionBlockIndexList[0].BlockHash.ShouldBe(blockIndex1.BlockHash);
                transactionBlockIndex.PreviousExecutionBlockIndexList[0].BlockHeight.ShouldBe(blockIndex1.BlockHeight);

                _transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId, out var transactionBlockIndexCache)
                    .ShouldBeTrue();
                transactionBlockIndexCache.ShouldBe(transactionBlockIndex);
            }

            var i = 0;
            var currentBlock = block2;
            while (i++ < KernelConstants.ReferenceBlockValidPeriod)
            {
                var block = await _kernelTestHelper.AttachBlock(currentBlock.Height, currentBlock.GetHash());
                // await AddBlockAsync(chain, block);
                currentBlock = block;
            }

            await SetBestChain(chain, currentBlock);
            await _blockchainService.SetIrreversibleBlockAsync(chain, currentBlock.Height, currentBlock.GetHash());
            await _transactionBlockIndexService.UpdateTransactionBlockIndicesByLibHeightAsync(currentBlock.Height);
            {
                _transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId, out _).ShouldBeFalse();

                var existsOnForkBranch =
                    await _transactionBlockIndexService.ValidateTransactionBlockIndexExistsInBranchAsync(txId,
                        block1.GetHash());
                Assert.False(existsOnForkBranch);
                var actualBlockIndexOnBestChain =
                    await _transactionBlockIndexService.GetTransactionBlockIndexAsync(txId);
                Assert.Equal(blockIndex2, actualBlockIndexOnBestChain);

                var transactionBlockIndex = await _transactionBlockIndexManager.GetTransactionBlockIndexAsync(txId);
                transactionBlockIndex.BlockHash.ShouldBe(blockIndex2.BlockHash);
                transactionBlockIndex.BlockHeight.ShouldBe(blockIndex2.BlockHeight);
                transactionBlockIndex.PreviousExecutionBlockIndexList.Count.ShouldBe(0);
            }
        }

        [Fact]
        public async Task AddBlockIndex_Repeated_Test()
        {
            var previousBlockHeader = _kernelTestHelper.BestBranchBlockList.Last().Header;
            var block1 = _kernelTestHelper.GenerateBlock(previousBlockHeader.Height, HashHelper.ComputeFrom("PreBlockHash1"));
            var block2 =
                _kernelTestHelper.GenerateBlock(previousBlockHeader.Height, previousBlockHeader.PreviousBlockHash);

            var txId = HashHelper.ComputeFrom("Transaction");

            var blockIndex1 = new BlockIndex
            {
                BlockHash = block1.GetHash(),
                BlockHeight = block1.Height
            };
            await _transactionBlockIndexService.AddBlockIndexAsync(new List<Hash> {txId}, blockIndex1);

            var blockIndex2 = new BlockIndex
            {
                BlockHash = block2.GetHash(),
                BlockHeight = block2.Height
            };
            await _transactionBlockIndexService.AddBlockIndexAsync(new List<Hash> {txId}, blockIndex2);
            
            var transactionBlockIndex = await _transactionBlockIndexManager.GetTransactionBlockIndexAsync(txId);
            
            await _transactionBlockIndexService.AddBlockIndexAsync(new List<Hash> {txId}, blockIndex1);
            var txBlockIndex = await _transactionBlockIndexManager.GetTransactionBlockIndexAsync(txId);
            txBlockIndex.ShouldBe(transactionBlockIndex);
            
            await _transactionBlockIndexService.AddBlockIndexAsync(new List<Hash> {txId}, blockIndex2);
            txBlockIndex = await _transactionBlockIndexManager.GetTransactionBlockIndexAsync(txId);
            txBlockIndex.ShouldBe(transactionBlockIndex);
        }

        [Fact]
        public async Task AddBlockIndex_Replace_Test()
        {
            var block1 = _kernelTestHelper.GenerateBlock(11, HashHelper.ComputeFrom("PreBlockHash1"));
            var block2 = _kernelTestHelper.GenerateBlock(12, HashHelper.ComputeFrom("PreBlockHash2"));
            var block3 = _kernelTestHelper.GenerateBlock(13, HashHelper.ComputeFrom("PreBlockHash3"));
            
            var txId = HashHelper.ComputeFrom("Transaction");
            
            var blockIndex1 = new BlockIndex
            {
                BlockHash = block1.GetHash(),
                BlockHeight = block1.Height
            };
            
            var blockIndex2 = new BlockIndex
            {
                BlockHash = block2.GetHash(),
                BlockHeight = block2.Height
            };
            
            var blockIndex3 = new BlockIndex
            {
                BlockHash = block3.GetHash(),
                BlockHeight = block3.Height
            };
            
            {
                await _transactionBlockIndexService.AddBlockIndexAsync(new List<Hash> {txId}, blockIndex2);

                var transactionBlockIndex = await _transactionBlockIndexManager.GetTransactionBlockIndexAsync(txId);
                transactionBlockIndex.BlockHash.ShouldBe(blockIndex2.BlockHash);
                transactionBlockIndex.BlockHeight.ShouldBe(blockIndex2.BlockHeight);
                transactionBlockIndex.PreviousExecutionBlockIndexList.Count.ShouldBe(0);
            }

            {
                await _transactionBlockIndexService.AddBlockIndexAsync(new List<Hash> {txId}, blockIndex1);
                
                var transactionBlockIndex = await _transactionBlockIndexManager.GetTransactionBlockIndexAsync(txId);
                transactionBlockIndex.BlockHash.ShouldBe(blockIndex2.BlockHash);
                transactionBlockIndex.BlockHeight.ShouldBe(blockIndex2.BlockHeight);
                transactionBlockIndex.PreviousExecutionBlockIndexList.Count.ShouldBe(1);
                transactionBlockIndex.PreviousExecutionBlockIndexList[0].BlockHash.ShouldBe(blockIndex1.BlockHash);
                transactionBlockIndex.PreviousExecutionBlockIndexList[0].BlockHeight.ShouldBe(blockIndex1.BlockHeight);
            }

            {
                await _transactionBlockIndexService.AddBlockIndexAsync(new List<Hash> {txId}, blockIndex3);
                var transactionBlockIndex = await _transactionBlockIndexManager.GetTransactionBlockIndexAsync(txId);
                transactionBlockIndex.BlockHash.ShouldBe(blockIndex3.BlockHash);
                transactionBlockIndex.BlockHeight.ShouldBe(blockIndex3.BlockHeight);
                transactionBlockIndex.PreviousExecutionBlockIndexList.Count.ShouldBe(2);
                transactionBlockIndex.PreviousExecutionBlockIndexList.ShouldContain(blockIndex1);
                transactionBlockIndex.PreviousExecutionBlockIndexList.ShouldContain(blockIndex2);
            }
        }

        [Fact]
        public async Task InitializeTransactionBlockIndexCache_Success()
        {
            var chain = await _blockchainService.GetChainAsync();

            var blockHash = chain.LastIrreversibleBlockHash;
            var blockHeight = chain.LastIrreversibleBlockHeight;
            while (true)
            {
                var block = await _blockchainService.GetBlockByHashAsync(blockHash);
                foreach (var txId in block.TransactionIds)
                {
                    await _transactionBlockIndexService.AddBlockIndexAsync(new List<Hash> {txId},
                        new BlockIndex
                        {
                            BlockHash = block.GetHash(),
                            BlockHeight = block.Height
                        });
                }

                if (blockHeight == AElfConstants.GenesisBlockHeight)
                    break;

                blockHash = block.Header.PreviousBlockHash;
                blockHeight--;
            }

            await _transactionBlockIndexService.LoadTransactionBlockIndexAsync();

            blockHeight = chain.LastIrreversibleBlockHeight;
            blockHash = chain.LastIrreversibleBlockHash;
            while (true)
            {
                var block = await _blockchainService.GetBlockByHashAsync(blockHash);
                foreach (var txId in block.TransactionIds)
                {
                    var blockIndex = await _transactionBlockIndexService.GetTransactionBlockIndexAsync(txId);
                    Assert.NotNull(blockIndex);
                }

                if (blockHeight == AElfConstants.GenesisBlockHeight || blockHeight <=
                    chain.LastIrreversibleBlockHeight - KernelConstants.ReferenceBlockValidPeriod)
                    break;

                blockHash = block.Header.PreviousBlockHash;
                blockHeight--;
            }
        }

        [Fact]
        public async Task UpdateTransactionBlockIndicesByLibHeight_Test()
        {
            var blockHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync();

            var txId = HashHelper.ComputeFrom("Transaction");
            var blockIndex = new BlockIndex
            {
                BlockHash = blockHeader.GetHash(),
                BlockHeight = blockHeader.Height
            };
            await _transactionBlockIndexService.AddBlockIndexAsync(new List<Hash> {txId}, blockIndex);

            await _transactionBlockIndexService.UpdateTransactionBlockIndicesByLibHeightAsync(blockHeader.Height);
            _transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId, out var transactionBlockIndex);
            transactionBlockIndex.BlockHash.ShouldBe(blockHeader.GetHash());
            transactionBlockIndex.BlockHeight.ShouldBe(blockHeader.Height);

            await _transactionBlockIndexService.UpdateTransactionBlockIndicesByLibHeightAsync(
                blockHeader.Height + KernelConstants.ReferenceBlockValidPeriod);
            _transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId, out transactionBlockIndex);
            transactionBlockIndex.ShouldBeNull();
        }

        private async Task AddBlockAsync(Chain chain, Block block)
        {
            await _blockchainService.AddBlockAsync(block);
            await _blockchainService.AttachBlockToChainAsync(chain, block);
        }

        private async Task SetBestChain(Chain chain, Block block)
        {
            await _blockchainService.SetBestChainAsync(chain, block.Height, block.GetHash());
        }
    }
}