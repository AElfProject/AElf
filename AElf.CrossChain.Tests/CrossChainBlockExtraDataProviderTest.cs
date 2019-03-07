using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
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

        public CrossChainBlockExtraDataProviderTest()
        {
            _crossChainBlockExtraDataProvider = GetRequiredService<IBlockExtraDataProvider>();
            _crossChainTestHelper = GetRequiredService<CrossChainTestHelper>();
        }
        private LogEvent CreateCrossChainLogEvent(byte[] topic, byte[] data)
        {
            return new LogEvent
            {
                Address = ContractHelpers.GetCrossChainContractAddress(0),
                Topics =
                {
                    ByteString.CopyFrom(topic)
                },
                Data = ByteString.CopyFrom(data)
            };
        }
        
        [Fact]
        public async Task FillExtraData_WithoutData()
        {
            var header = new BlockHeader
            {
                Height = 1
            };
            await _crossChainBlockExtraDataProvider.FillExtraDataAsync(header);
            Assert.Empty(header.BlockExtraDatas);
        }
        
        
        [Fact]
        public async Task FillExtraData_OneEvent()
        {
            var fakeIndexedCrossChainBlockData = new CrossChainBlockData();

            var fakeMerkleTreeRoot1 = Hash.FromString("fakeMerkleTreeRoot1");
            var fakeMerkleTreeRoot2 = Hash.FromString("fakeMerkleTreeRoot2");
            var fakeMerkleTreeRoot3 = Hash.FromString("fakeMerkleTreeRoot3");
            
            var fakeSideChainBlockDataList = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    SideChainHeight = 1,
                    TransactionMKRoot = fakeMerkleTreeRoot1
                },
                new SideChainBlockData
                {
                    SideChainHeight = 1,
                    TransactionMKRoot = fakeMerkleTreeRoot2
                },
                new SideChainBlockData
                {
                    SideChainHeight = 1,
                    TransactionMKRoot = fakeMerkleTreeRoot3
                }
            };
            fakeIndexedCrossChainBlockData.SideChainBlockData.AddRange(fakeSideChainBlockDataList);
            _crossChainTestHelper.AddFakeIndexedCrossChainBlockData(1, fakeIndexedCrossChainBlockData);
            var header = new BlockHeader
            {
                Height = 1
            };

            var sideChainTxMerkleTreeRoot = await _crossChainBlockExtraDataProvider.FillExtraDataAsync(header);
            var expected = new BinaryMerkleTree()
                .AddNodes(fakeSideChainBlockDataList.Select(sideChainBlockData => sideChainBlockData.TransactionMKRoot))
                .ComputeRootHash().ToByteString();
            Assert.Equal(expected, sideChainTxMerkleTreeRoot);
        }
    }
}