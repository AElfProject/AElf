using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Xunit;

namespace AElf.CrossChain
{
    public class CrossChainValidationProviderTest : CrossChainTestBase
    {
        private readonly IBlockValidationProvider _crossChainBlockValidationProvider;
        private readonly CrossChainTestHelper _crossChainTestHelper;
        public CrossChainValidationProviderTest()
        {
            _crossChainBlockValidationProvider = GetRequiredService<IBlockValidationProvider>();
            _crossChainTestHelper = GetRequiredService<CrossChainTestHelper>();
        }

        [Fact]
        public async Task Validate_GenesisHeight()
        {
            var block = new Block
            {
                Header = new BlockHeader
                {
                    Height = 1
                }
            };
            var validationRes = await _crossChainBlockValidationProvider.ValidateBlockAfterExecuteAsync(block);
            Assert.True(validationRes);
        }
        
        [Fact]
        public async Task Validate_EmptyHeader_NoIndexedData()
        {
            var block = new Block
            {
                Header = new BlockHeader
                {
                    Height = 2
                }
            };
            var validationRes = await _crossChainBlockValidationProvider.ValidateBlockAfterExecuteAsync(block);
            Assert.True(validationRes);
        }
        
        [Fact]
        public async Task Validate_EmptyHeader_WithIndexedData()
        {
            var block = new Block
            {
                Header = new BlockHeader
                {
                    Height = 2
                }
            };
            
            var fakeMerkleTreeRoot1 = Hash.FromString("fakeMerkleTreeRoot1");
            int chainId = 123;
            var fakeSideChainBlockData = new SideChainBlockData
            {
                SideChainHeight = 1,
                TransactionMerkleTreeRoot = fakeMerkleTreeRoot1,
                SideChainId = chainId
            };
            CreateFakeCacheAndStateData(chainId, fakeSideChainBlockData, block.Height);
            await Assert.ThrowsAsync<ValidateNextTimeBlockValidationException>(() =>
                _crossChainBlockValidationProvider.ValidateBlockAfterExecuteAsync(block));
        }

        [Fact]
        public async Task Validate_WithoutCache()
        {
            var fakeMerkleTreeRoot1 = Hash.FromString("fakeMerkleTreeRoot1");
            int chainId = 123;
            var sideChainBlockData = new SideChainBlockData
            {
                SideChainHeight = 1,
                TransactionMerkleTreeRoot = fakeMerkleTreeRoot1,
                SideChainId = chainId
            };
            var sideChainTxMerkleTreeRoot = ComputeRootHash(new []{sideChainBlockData});
            var block = CreateFilledBlock(sideChainTxMerkleTreeRoot);
            var fakeIndexedCrossChainData = new CrossChainBlockData();
            fakeIndexedCrossChainData.SideChainBlockData.Add(sideChainBlockData);
            _crossChainTestHelper.AddFakeIndexedCrossChainBlockData(2, fakeIndexedCrossChainData);
           
            await Assert.ThrowsAsync<ValidateNextTimeBlockValidationException>(() =>
                _crossChainBlockValidationProvider.ValidateBlockAfterExecuteAsync(block));
        }
        
        [Fact]
        public async Task Validate_IncompatibleExtraData()
        {
            var fakeMerkleTreeRoot1 = Hash.FromString("fakeMerkleTreeRoot1");
            var fakeSideChainId = 123;
            var fakeSideChainBlockData = new SideChainBlockData
            {
                SideChainHeight = 1,
                TransactionMerkleTreeRoot = fakeMerkleTreeRoot1,
                SideChainId = fakeSideChainId
            };
            CreateFakeCacheAndStateData(fakeSideChainId, fakeSideChainBlockData, 2);
            
            // mock data in cache
            var fakeTxnMerkleTreeRoot = Hash.FromString("fakeMerkleTreeRoot2");

            var block = CreateFilledBlock(fakeTxnMerkleTreeRoot);
            await Assert.ThrowsAsync<ValidateNextTimeBlockValidationException>(() =>
                _crossChainBlockValidationProvider.ValidateBlockAfterExecuteAsync(block));
        }
        
        [Fact]
        public async Task Validate_IncompatibleCacheData()
        {
            var fakeMerkleTreeRoot1 = Hash.FromString("fakeMerkleTreeRoot1");
            var fakeSideChainId = 123;
            var fakeSideChainBlockData = new SideChainBlockData
            {
                SideChainHeight = 1,
                TransactionMerkleTreeRoot = fakeMerkleTreeRoot1,
                SideChainId = fakeSideChainId
            };
            
            var fakeTxnMerkleTreeRoot2 = Hash.FromString("fakeMerkleTreeRoot2");
            var fakeSideChainBlockData2 = new SideChainBlockData
            {
                SideChainHeight = 1,
                TransactionMerkleTreeRoot = fakeTxnMerkleTreeRoot2,
                SideChainId = fakeSideChainId
            };
            
            CreateFakeCacheAndStateData(fakeSideChainId, fakeSideChainBlockData2, 2);
            var sideChainTxMerkleTreeRoot = ComputeRootHash(new []{fakeSideChainBlockData});
            var block = CreateFilledBlock(sideChainTxMerkleTreeRoot);
            await Assert.ThrowsAsync<ValidateNextTimeBlockValidationException>(() =>
                _crossChainBlockValidationProvider.ValidateBlockAfterExecuteAsync(block));
        }
        
        [Fact]
        public async Task Validate()
        {
            var fakeMerkleTreeRoot1 = Hash.FromString("fakeMerkleTreeRoot1");
            var fakeSideChainId = 123;
            var fakeSideChainBlockData = new SideChainBlockData
            {
                SideChainHeight = 1,
                TransactionMerkleTreeRoot = fakeMerkleTreeRoot1,
                SideChainId = fakeSideChainId
            };
            
            CreateFakeCacheAndStateData(fakeSideChainId, fakeSideChainBlockData, 2);
            var sideChainTxMerkleTreeRoot = ComputeRootHash(new []{fakeSideChainBlockData});
            var block = CreateFilledBlock(sideChainTxMerkleTreeRoot);
            var res = await _crossChainBlockValidationProvider.ValidateBlockAfterExecuteAsync(block);
            Assert.True(res);
        }

        private IBlock CreateFilledBlock(Hash merkleTreeRoot)
        {
            var block = new Block
            {
                Header = new BlockHeader
                {
                    Height = 2
                }
            };
            
            block.Header.BlockExtraDatas.Add(new CrossChainExtraData{SideChainTransactionsRoot = merkleTreeRoot}.ToByteString());
            return block;
        }

        private Hash ComputeRootHash(IEnumerable<SideChainBlockData> blockInfo)
        {
            return new BinaryMerkleTree()
                .AddNodes(blockInfo.Select(sideChainBlockData => sideChainBlockData.TransactionMerkleTreeRoot))
                .ComputeRootHash();
        }

        private void CreateFakeCacheAndStateData(int fakeSideChainId, SideChainBlockData fakeSideChainBlockData, long height = 1)
        {
            var fakeIndexedCrossChainBlockData = new CrossChainBlockData();
            fakeIndexedCrossChainBlockData.SideChainBlockData.AddRange(new []{fakeSideChainBlockData});

            // mock data in state
            _crossChainTestHelper.AddFakeIndexedCrossChainBlockData(height, fakeIndexedCrossChainBlockData);
            _crossChainTestHelper.AddFakeSideChainIdHeight(fakeSideChainId, 0);
            
            // mock data in cache
            AddFakeCacheData(new Dictionary<int, List<IBlockInfo>>
            {
                {fakeSideChainId, new List<IBlockInfo>{fakeSideChainBlockData}}
            });
        }
    }
}