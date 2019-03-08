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
        public async Task Validate_EmptyHeader()
        {
            var block = new Block
            {
                Header = new BlockHeader
                {
                    Height = 1
                }
            };
            _crossChainTestHelper.AddFakeIndexedCrossChainBlockData(1, new CrossChainBlockData());
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                _crossChainBlockValidationProvider.ValidateBlockAfterExecuteAsync(block));
        }

        [Fact]
        public async Task Validate_WithoutCache()
        {
            var fakeMerkleTreeRoot1 = Hash.FromString("fakeMerkleTreeRoot1");
            int chainId = 123;
            var fakeIndexedCrossChainBlockData = new CrossChainBlockData();
            var fakeSideChainBlockDataList = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    SideChainHeight = 1,
                    TransactionMKRoot = fakeMerkleTreeRoot1,
                    SideChainId = chainId
                }
            };
            fakeIndexedCrossChainBlockData.SideChainBlockData.AddRange(fakeSideChainBlockDataList);
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 1);
            _crossChainTestHelper.AddFakeIndexedCrossChainBlockData(1, fakeIndexedCrossChainBlockData);
            var sideChainTxMerkleTreeRoot = ComputeRootHash(fakeSideChainBlockDataList);

            var block = CreateFilledBlock(ByteString.CopyFrom(sideChainTxMerkleTreeRoot.DumpByteArray()));
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
                TransactionMKRoot = fakeMerkleTreeRoot1,
                SideChainId = fakeSideChainId
            };
            CreateFakeCacheAndStateData(fakeSideChainId, fakeSideChainBlockData);
            
            // mock data in cache
            var fakeTxnMerkleTreeRoot = Hash.FromString("fakeMerkleTreeRoot2");

            var block = CreateFilledBlock(ByteString.CopyFrom(fakeTxnMerkleTreeRoot.DumpByteArray()));
            await Assert.ThrowsAsync<ValidateNextTimeBlockValidationException>(() =>
                _crossChainBlockValidationProvider.ValidateBlockAfterExecuteAsync(block));
        }
        
        [Fact]
        public async Task Validate_IncompatibleCacheData()
        {
            var fakeMerkleTreeRoot1 = Hash.FromString("fakeMerkleTreeRoot1");
            var fakeSideChainId = 123;
            var fakeIndexedCrossChainBlockData = new CrossChainBlockData();
            var fakeSideChainBlockData = new SideChainBlockData
            {
                SideChainHeight = 1,
                TransactionMKRoot = fakeMerkleTreeRoot1,
                SideChainId = fakeSideChainId
            };
            
            fakeIndexedCrossChainBlockData.SideChainBlockData.AddRange(new []{fakeSideChainBlockData});
            // mock data in state
            _crossChainTestHelper.AddFakeIndexedCrossChainBlockData(1, fakeIndexedCrossChainBlockData);
            _crossChainTestHelper.AddFakeSideChainIdHeight(fakeSideChainId, 1);

            var fakeSideChainId2 = 124;
            var fakeSideChainBlockData2 = new SideChainBlockData
            {
                SideChainHeight = 1,
                TransactionMKRoot = fakeMerkleTreeRoot1,
                SideChainId = fakeSideChainId2
            };
            // mock data in cache
            AddFakeCacheData(new Dictionary<int, List<IBlockInfo>>
            {
                {fakeSideChainId, new List<IBlockInfo>{fakeSideChainBlockData2}}
            });
            var sideChainTxMerkleTreeRoot = ComputeRootHash(new []{fakeSideChainBlockData});
            var block = CreateFilledBlock(ByteString.CopyFrom(sideChainTxMerkleTreeRoot.DumpByteArray()));
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
                TransactionMKRoot = fakeMerkleTreeRoot1,
                SideChainId = fakeSideChainId
            };
            
            CreateFakeCacheAndStateData(fakeSideChainId, fakeSideChainBlockData);
            var sideChainTxMerkleTreeRoot = ComputeRootHash(new []{fakeSideChainBlockData});
            var block = CreateFilledBlock(ByteString.CopyFrom(sideChainTxMerkleTreeRoot.DumpByteArray()));
            var res = await _crossChainBlockValidationProvider.ValidateBlockAfterExecuteAsync(block);
            Assert.True(res);
        }

        private IBlock CreateFilledBlock(ByteString extraData)
        {
            var block = new Block
            {
                Header = new BlockHeader
                {
                    Height = 1
                }
            };
            
            block.Header.BlockExtraDatas.Add(extraData);
            return block;
        }

        private Hash ComputeRootHash(IEnumerable<SideChainBlockData> blockInfo)
        {
            return new BinaryMerkleTree()
                .AddNodes(blockInfo.Select(sideChainBlockData => sideChainBlockData.TransactionMKRoot))
                .ComputeRootHash();
        }

        private void CreateFakeCacheAndStateData(int fakeSideChainId, SideChainBlockData fakeSideChainBlockData)
        {
            var fakeIndexedCrossChainBlockData = new CrossChainBlockData();
            fakeIndexedCrossChainBlockData.SideChainBlockData.AddRange(new []{fakeSideChainBlockData});

            // mock data in state
            _crossChainTestHelper.AddFakeIndexedCrossChainBlockData(1, fakeIndexedCrossChainBlockData);
            _crossChainTestHelper.AddFakeSideChainIdHeight(fakeSideChainId, 1);
            
            // mock data in cache
            AddFakeCacheData(new Dictionary<int, List<IBlockInfo>>
            {
                {fakeSideChainId, new List<IBlockInfo>{fakeSideChainBlockData}}
            });
        }
    }
}