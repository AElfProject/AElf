using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Xunit;

namespace AElf.Kernel.Blockchain.Application
{
    public class TransactionBlockIndexServiceTests : AElfKernelWithChainTestBase
    {
        private readonly ITransactionBlockIndexService _transactionBlockIndexService;
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly IBlockchainService _blockchainService;

        public TransactionBlockIndexServiceTests()
        {
            _transactionBlockIndexService = GetRequiredService<ITransactionBlockIndexService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _blockchainService = GetRequiredService<IBlockchainService>();
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
            var block1 = _kernelTestHelper.GenerateBlock(previousBlockHeader.Height, HashHelper.ComputeFrom("PreBlockHash1"));
            var block2 =
                _kernelTestHelper.GenerateBlock(previousBlockHeader.Height, previousBlockHeader.PreviousBlockHash);

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

            {
                var existsOnForkBranch =
                    await _transactionBlockIndexService.ValidateTransactionBlockIndexExistsInBranchAsync(txId,
                        block1.GetHash());
                Assert.True(existsOnForkBranch);
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
                var existsOnForkBranch =
                    await _transactionBlockIndexService.ValidateTransactionBlockIndexExistsInBranchAsync(txId,
                        block1.GetHash());
                Assert.False(existsOnForkBranch);
                var actualBlockIndexOnBestChain =
                    await _transactionBlockIndexService.GetTransactionBlockIndexAsync(txId);
                Assert.Equal(blockIndex2, actualBlockIndexOnBestChain);
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