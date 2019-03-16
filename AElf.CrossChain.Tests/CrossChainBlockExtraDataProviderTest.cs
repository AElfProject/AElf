using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.CrossChain.Cache;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
using Google.Protobuf;
using Xunit;

namespace AElf.CrossChain
{
    public class CrossChainBlockExtraDataProviderTest : CrossChainTestBase
    {
        private readonly IBlockExtraDataProvider _crossChainBlockExtraDataProvider;
        private readonly CrossChainTestHelper _crossChainTestHelper;
        private readonly ICrossChainDataConsumer _crossChainDataConsumer;
        private readonly ICrossChainDataProducer _crossChainDataProducer;
        public CrossChainBlockExtraDataProviderTest()
        {
            _crossChainBlockExtraDataProvider = GetRequiredService<IBlockExtraDataProvider>();
            _crossChainTestHelper = GetRequiredService<CrossChainTestHelper>();
            _crossChainDataConsumer = GetRequiredService<ICrossChainDataConsumer>();
            _crossChainDataProducer = GetRequiredService<ICrossChainDataProducer>();
        }
        
        [Fact(Skip = "Return value would be null.")]
        public async Task FillExtraData_WithoutData()
        {
            var header = new BlockHeader
            {
                Height = 1
            };
            await _crossChainBlockExtraDataProvider.GetExtraDataForFillingBlockHeaderAsync(header);
            Assert.Empty(header.BlockExtraDatas);
        }
        
        
        [Fact]
        public async Task FillExtraData_GenesisHeight()
        {
            var header = new BlockHeader
            {
                Height = 1
            };
            var bytes = await _crossChainBlockExtraDataProvider.GetExtraDataForFillingBlockHeaderAsync(header);
            Assert.Empty(bytes);
        }
        
        [Fact]
        public async Task FillExtraData()
        {
            var fakeMerkleTreeRoot1 = Hash.FromString("fakeMerkleTreeRoot1");
            var fakeMerkleTreeRoot2 = Hash.FromString("fakeMerkleTreeRoot2");
            var fakeMerkleTreeRoot3 = Hash.FromString("fakeMerkleTreeRoot3");

            int chainId1 = ChainHelpers.GetRandomChainId();
            int chainId2 = ChainHelpers.GetRandomChainId();
            int chainId3 = ChainHelpers.GetRandomChainId();

            _crossChainDataConsumer.TryRegisterNewChainCache(chainId1);
            _crossChainDataConsumer.TryRegisterNewChainCache(chainId2);
            _crossChainDataConsumer.TryRegisterNewChainCache(chainId3);
            var fakeSideChainBlockDataList = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    SideChainHeight = 1,
                    TransactionMKRoot = fakeMerkleTreeRoot1,
                    SideChainId = chainId1
                },
                new SideChainBlockData
                {
                    SideChainHeight = 1,
                    TransactionMKRoot = fakeMerkleTreeRoot2,
                    SideChainId = chainId2
                },
                new SideChainBlockData
                {
                    SideChainHeight = 1,
                    TransactionMKRoot = fakeMerkleTreeRoot3,
                    SideChainId = chainId3
                }
            };
            
            _crossChainDataProducer.AddNewBlockInfo(fakeSideChainBlockDataList[0]);
            _crossChainDataProducer.AddNewBlockInfo(fakeSideChainBlockDataList[1]);          
            _crossChainDataProducer.AddNewBlockInfo(fakeSideChainBlockDataList[2]);

            for (int i = 2; i < CrossChainConsts.MinimalBlockInfoCacheThreshold + 2; i++)
            {
                _crossChainDataProducer.AddNewBlockInfo(new SideChainBlockData
                {
                    SideChainHeight = i,
                    TransactionMKRoot = fakeMerkleTreeRoot1,
                    SideChainId = chainId1
                });
                _crossChainDataProducer.AddNewBlockInfo(new SideChainBlockData
                {
                    SideChainHeight = i,
                    TransactionMKRoot = fakeMerkleTreeRoot2,
                    SideChainId = chainId2
                });
                _crossChainDataProducer.AddNewBlockInfo(new SideChainBlockData
                {
                    SideChainHeight = i,
                    TransactionMKRoot = fakeMerkleTreeRoot3,
                    SideChainId = chainId3
                });
                
            }
            
//            fakeIndexedCrossChainBlockData.SideChainBlockData.AddRange(fakeSideChainBlockDataList);
//            _crossChainTestHelper.AddFakeIndexedCrossChainBlockData(1, fakeIndexedCrossChainBlockData);
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId1, 0);
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId2, 0);
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId3, 0);
            _crossChainTestHelper.SetFakeLibHeight(1);
            var header = new BlockHeader
            {
                PreviousBlockHash = Hash.FromString("PrevioudHash"),
                Height = 2
            };

            var sideChainTxMerkleTreeRoot =
                await _crossChainBlockExtraDataProvider.GetExtraDataForFillingBlockHeaderAsync(header);
            var merkleTreeRoot = new BinaryMerkleTree()
                .AddNodes(fakeSideChainBlockDataList.Select(sideChainBlockData => sideChainBlockData.TransactionMKRoot))
                .ComputeRootHash();
            var expected = new CrossChainExtraData {SideChainTransactionsRoot = merkleTreeRoot}.ToByteString();
            Assert.Equal(expected, sideChainTxMerkleTreeRoot);
        }
    }
}