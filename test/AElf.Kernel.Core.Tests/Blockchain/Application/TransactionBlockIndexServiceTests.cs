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
            var block = _kernelTestHelper.GenerateBlock(previousBlockHeader.Height, Hash.FromString("PreBlockHash"));
            Hash txId = Hash.FromString("Transaction"); 
            var blockIndex = new BlockIndex
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            };
            await _transactionBlockIndexService.UpdateTransactionBlockIndexAsync(txId, blockIndex);
            
            var actual = await _transactionBlockIndexService.GetTransactionBlockIndexAsync(txId);
            Assert.Null(actual);
        }
        
        [Fact]
        public async Task UpdateOneBlockIndexWithBestChain()
        {
            var previousBlockHeader = _kernelTestHelper.BestBranchBlockList.Last().Header;
            var block = _kernelTestHelper.GenerateBlock(previousBlockHeader.Height, Hash.FromString("PreBlockHash"));
            Hash txId = Hash.FromString("Transaction"); 
            var blockIndex = new BlockIndex
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            };
            await _transactionBlockIndexService.UpdateTransactionBlockIndexAsync(txId, blockIndex);
            var chain = await _blockchainService.GetChainAsync();
            await SetBestChain(chain, block);

            var actual = await _transactionBlockIndexService.GetTransactionBlockIndexAsync(txId);
            Assert.Equal(blockIndex, actual);
        }
        
        [Fact]
        public async Task UpdateTwoBlockIndexesWithoutBestChain()
        {
            var previousBlockHeader = _kernelTestHelper.BestBranchBlockList.Last().Header;
            var block1 = _kernelTestHelper.GenerateBlock(previousBlockHeader.Height, Hash.FromString("PreBlockHash1"));
            var block2 = _kernelTestHelper.GenerateBlock(previousBlockHeader.Height, Hash.FromString("PreBlockHash2"));

            Hash txId = Hash.FromString("Transaction"); 
            var blockIndex1 = new BlockIndex
            {
                BlockHash = block1.GetHash(),
                BlockHeight = block1.Height
            };
            await _transactionBlockIndexService.UpdateTransactionBlockIndexAsync(txId, blockIndex1);
            
            var blockIndex2 = new BlockIndex
            {
                BlockHash = block2.GetHash(),
                BlockHeight = block2.Height
            };
            await _transactionBlockIndexService.UpdateTransactionBlockIndexAsync(txId, blockIndex2);
            
            var actual = await _transactionBlockIndexService.GetTransactionBlockIndexAsync(txId);
            Assert.Null(actual);
        }
        
        [Fact]
        public async Task UpdateTwoBlockIndexesWithFirstBestChain()
        {
            var previousBlockHeader = _kernelTestHelper.BestBranchBlockList.Last().Header;
            var block1 = _kernelTestHelper.GenerateBlock(previousBlockHeader.Height, Hash.FromString("PreBlockHash1"));
            var block2 = _kernelTestHelper.GenerateBlock(previousBlockHeader.Height, Hash.FromString("PreBlockHash2"));

            Hash txId = Hash.FromString("Transaction"); 
            var blockIndex1 = new BlockIndex
            {
                BlockHash = block1.GetHash(),
                BlockHeight = block1.Height
            };
            await _transactionBlockIndexService.UpdateTransactionBlockIndexAsync(txId, blockIndex1);
            
            var blockIndex2 = new BlockIndex
            {
                BlockHash = block2.GetHash(),
                BlockHeight = block2.Height
            };
            await _transactionBlockIndexService.UpdateTransactionBlockIndexAsync(txId, blockIndex2);
            
            var chain = await _blockchainService.GetChainAsync();
            await SetBestChain(chain, block1);

            var actual = await _transactionBlockIndexService.GetTransactionBlockIndexAsync(txId);
            Assert.Equal(blockIndex1, actual);
        }
        
        [Fact]
        public async Task UpdateTwoBlockIndexesWithSecondBestChain()
        {
            var previousBlockHeader = _kernelTestHelper.BestBranchBlockList.Last().Header;
            var block1 = _kernelTestHelper.GenerateBlock(previousBlockHeader.Height, Hash.FromString("PreBlockHash1"));
            var block2 = _kernelTestHelper.GenerateBlock(previousBlockHeader.Height, Hash.FromString("PreBlockHash2"));

            Hash txId = Hash.FromString("Transaction"); 
            var blockIndex1 = new BlockIndex
            {
                BlockHash = block1.GetHash(),
                BlockHeight = block1.Height
            };
            await _transactionBlockIndexService.UpdateTransactionBlockIndexAsync(txId, blockIndex1);
            
            var blockIndex2 = new BlockIndex
            {
                BlockHash = block2.GetHash(),
                BlockHeight = block2.Height
            };
            await _transactionBlockIndexService.UpdateTransactionBlockIndexAsync(txId, blockIndex2);
            
            var chain = await _blockchainService.GetChainAsync();
            await SetBestChain(chain, block2);

            var actual = await _transactionBlockIndexService.GetTransactionBlockIndexAsync(txId);
            Assert.Equal(blockIndex2, actual);
        }

        [Fact]
        public async Task UpdateTwoBlockIndexesWithTwiceBestChain()
        {
            var previousBlockHeader = _kernelTestHelper.BestBranchBlockList.Last().Header;
            var block1 = _kernelTestHelper.GenerateBlock(previousBlockHeader.Height, Hash.FromString("PreBlockHash1"));
            var block2 = _kernelTestHelper.GenerateBlock(previousBlockHeader.Height, Hash.FromString("PreBlockHash2"));

            Hash txId = Hash.FromString("Transaction"); 
            var blockIndex1 = new BlockIndex
            {
                BlockHash = block1.GetHash(),
                BlockHeight = block1.Height
            };
            await _transactionBlockIndexService.UpdateTransactionBlockIndexAsync(txId, blockIndex1);
            var chain1 = await _blockchainService.GetChainAsync();
            await SetBestChain(chain1, block1);
            var actual1 = await _transactionBlockIndexService.GetTransactionBlockIndexAsync(txId);
            Assert.Equal(blockIndex1, actual1);
            
            var blockIndex2 = new BlockIndex
            {
                BlockHash = block2.GetHash(),
                BlockHeight = block2.Height
            };
            await _transactionBlockIndexService.UpdateTransactionBlockIndexAsync(txId, blockIndex2);
            
            var chain = await _blockchainService.GetChainAsync();
            await SetBestChain(chain, block2);

            var actual = await _transactionBlockIndexService.GetTransactionBlockIndexAsync(txId);
            Assert.Equal(blockIndex2, actual);
        }
        
        
        private async Task SetBestChain(Chain chain, Block block)
        {
            await _blockchainService.AddBlockAsync(block);
            await _blockchainService.AttachBlockToChainAsync(chain, block);
            await _blockchainService.SetBestChainAsync(chain, block.Height, block.GetHash());
        }
    }
}