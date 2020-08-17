using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Application
{
    public class BlockSyncValidationServiceInvalidBlockTests : BlockSyncAttachBlockAbnormalPeerTestBase
    {
        private readonly IBlockSyncValidationService _blockSyncValidationService;
        private readonly OSTestHelper _osTestHelper;

        public BlockSyncValidationServiceInvalidBlockTests()
        {
            _blockSyncValidationService = GetRequiredService<IBlockSyncValidationService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
        }

        [Fact]
        public async Task ValidateBlockBeforeAttach_InvalidBlock_ReturnFalse()
        {
            var invalidBlock = _osTestHelper.GenerateBlockWithTransactions(HashHelper.ComputeFrom("BadBlock"), 10000);
            var result = await _blockSyncValidationService.ValidateBlockBeforeAttachAsync(invalidBlock);
            result.ShouldBeFalse();
        }
    }
}