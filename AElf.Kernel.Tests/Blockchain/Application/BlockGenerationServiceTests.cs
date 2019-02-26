using System.Threading.Tasks;
using AElf.Common;
using Google.Protobuf.WellKnownTypes;
using Org.BouncyCastle.Asn1.Cms;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Blockchain.Application
{
    public class BlockGenerationServiceTests : AElfKernelTestBase
    {
        private readonly BlockGenerationService _blockGenerationService;

        public BlockGenerationServiceTests()
        {
            _blockGenerationService = GetRequiredService<BlockGenerationService>();
        }

        [Fact]
        public async Task Generate_Block_Success()
        {
            var generateBlockDto = new GenerateBlockDto
            {
                ChainId = 1,
                PreviousBlockHash = Hash.Genesis,
                PreviousBlockHeight = 1
            };

            var block = await _blockGenerationService.GenerateBlockAsync(generateBlockDto);

            block.Header.ChainId.ShouldBe(generateBlockDto.ChainId);
            block.Header.Height.ShouldBe(generateBlockDto.PreviousBlockHeight + 1);
            block.Header.PreviousBlockHash.ShouldBe(generateBlockDto.PreviousBlockHash);
            block.Header.Time.ShouldBe(Timestamp.FromDateTime(generateBlockDto.BlockTime));
        }
    }
}