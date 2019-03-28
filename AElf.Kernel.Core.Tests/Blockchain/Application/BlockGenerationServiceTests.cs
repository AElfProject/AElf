using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Infrastructure;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Blockchain.Application
{
    public class BlockGenerationServiceTests : AElfKernelTestBase
    {
        public BlockGenerationServiceTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _blockGenerationService = GetRequiredService<BlockGenerationService>();
            _staticChainInformationProvider = GetRequiredService<IStaticChainInformationProvider>();
        }

        private readonly BlockGenerationService _blockGenerationService;

        private readonly IBlockchainService _blockchainService;

        private readonly IStaticChainInformationProvider _staticChainInformationProvider;

        [Fact]
        public async Task Generate_Block_Success()
        {
            var generateBlockDto = new GenerateBlockDto
            {
                PreviousBlockHash = Hash.Empty,
                PreviousBlockHeight = 1
            };

            var block = await _blockGenerationService.GenerateBlockBeforeExecutionAsync(generateBlockDto);

            block.Header.ChainId.ShouldBe(_staticChainInformationProvider.ChainId);
            block.Header.Height.ShouldBe(generateBlockDto.PreviousBlockHeight + 1);
            block.Header.PreviousBlockHash.ShouldBe(generateBlockDto.PreviousBlockHash);
            block.Header.Time.ShouldBe(Timestamp.FromDateTime(generateBlockDto.BlockTime));
        }
    }
}