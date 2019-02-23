using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf.WellKnownTypes;
using Org.BouncyCastle.Asn1.Cms;
using Shouldly;
using Xunit;

namespace AElf.Kernel
{
    public class BlockGenerationServiceTests : AElfKernelTestBase
    {
        private readonly BlockGenerationService _blockGenerationService;

        public BlockGenerationServiceTests()
        {
            _blockGenerationService = GetRequiredService<BlockGenerationService>();
        }

        [Fact]
        public async Task GenerateBlock_Success()
        {
            var generateBlockDto = new GenerateBlockDto
            {
                ChainId = 0,
                PreviousBlockHash = Hash.Genesis,
                PreviousBlockHeight = 0
            };

            var block = await _blockGenerationService.GenerateBlockAsync(generateBlockDto);

            block.Header.ChainId.ShouldBe(generateBlockDto.ChainId);
            block.Header.Height.ShouldBe(generateBlockDto.PreviousBlockHeight + 1);
            block.Header.PreviousBlockHash.ShouldBe(generateBlockDto.PreviousBlockHash);
            block.Header.Time.ShouldBe(Timestamp.FromDateTime(generateBlockDto.BlockTime));
        }
    }
}