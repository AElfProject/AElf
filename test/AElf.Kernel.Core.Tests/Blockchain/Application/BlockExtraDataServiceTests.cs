using System.Threading.Tasks;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Blockchain.Application
{
    public class BlockExtraDataServiceTests: AElfMinerTestBase
    {
        private readonly IBlockExtraDataService _blockExtraDataService;

        public BlockExtraDataServiceTests()
        {
            _blockExtraDataService = GetRequiredService<IBlockExtraDataService>();
        }

        [Fact]
        public void GetBlockExtraData_Test()
        {
            var blockHeader = new BlockHeader()
            {
                Height = 2, // no extra data in genesis block
                ExtraData = { ByteString.CopyFromUtf8("test1") }
            };
            var queryResult = _blockExtraDataService.GetExtraDataFromBlockHeader("IBlockExtraDataProvider", blockHeader);
            queryResult.ShouldBe(blockHeader.ExtraData[0]);
            
            var queryResult1 = _blockExtraDataService.GetExtraDataFromBlockHeader("ConsensusExtraDataProvider", blockHeader);
            queryResult1.ShouldBeNull();
        }

        [Fact]
        public async Task FillBlockExtraData_Test()
        {
            var blockHeader = new BlockHeader()
            {
                Height = 100,
            };
            await _blockExtraDataService.FillBlockExtraData(blockHeader);
            blockHeader.ExtraData.Count.ShouldBe(0);

            blockHeader.Height = 1;
            await _blockExtraDataService.FillBlockExtraData(blockHeader);
            blockHeader.ExtraData.Count.ShouldBe(1);
        }
    }
}