using System.Threading.Tasks;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Blockchain.Application
{
    public class BlockExtraDataServiceTests : AElfMinerTestBase
    {
        public BlockExtraDataServiceTests()
        {
            _blockExtraDataProvider = GetRequiredService<IBlockExtraDataProvider>();
            _blockExtraDataService = new BlockExtraDataService(new[] {_blockExtraDataProvider});
        }

        private readonly IBlockExtraDataProvider _blockExtraDataProvider;
        private readonly IBlockExtraDataService _blockExtraDataService;

        [Fact]
        public async Task FillBlockExtraData_Test()
        {
            var blockHeader = new BlockHeader
            {
                Height = 100
            };
            await _blockExtraDataService.FillBlockExtraData(blockHeader);
            blockHeader.BlockExtraDatas.Count.ShouldBe(0);

            blockHeader.Height = 1;
            await _blockExtraDataService.FillBlockExtraData(blockHeader);
            blockHeader.BlockExtraDatas.Count.ShouldBe(1);
        }

        [Fact]
        public void GetBlockExtraData_Test()
        {
            var blockHeader = new BlockHeader
            {
                Height = 1,
                BlockExtraDatas = {ByteString.CopyFromUtf8("test1")}
            };
            var queryResult =
                _blockExtraDataService.GetExtraDataFromBlockHeader("IBlockExtraDataProvider", blockHeader);
            queryResult.ShouldBe(blockHeader.BlockExtraDatas[0]);

            var queryResult1 =
                _blockExtraDataService.GetExtraDataFromBlockHeader("ConsensusExtraDataProvider", blockHeader);
            queryResult1.ShouldBeNull();
        }
    }
}