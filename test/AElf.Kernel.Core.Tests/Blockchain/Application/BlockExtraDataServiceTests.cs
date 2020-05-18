using System.Linq;
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
                ExtraData = { {"ExtraDataKey", ByteString.CopyFromUtf8("test1")} }
            };
            var queryResult = _blockExtraDataService.GetExtraDataFromBlockHeader("ExtraDataKey", blockHeader);
            queryResult.ShouldBe(blockHeader.ExtraData.First().Value);
            
            var queryResult1 = _blockExtraDataService.GetExtraDataFromBlockHeader("NotExistExtraDataKey", blockHeader);
            queryResult1.ShouldBeNull();
        }

        [Fact]
        public async Task FillBlockExtraData_Test()
        {
            var blockHeader = new BlockHeader()
            {
                Height = 100,
            };
            await _blockExtraDataService.FillBlockExtraDataAsync(blockHeader);
            blockHeader.ExtraData.Count.ShouldBe(0);

            blockHeader.Height = 1;
            await _blockExtraDataService.FillBlockExtraDataAsync(blockHeader);
            blockHeader.ExtraData.Count.ShouldBe(1);
        }
    }
}