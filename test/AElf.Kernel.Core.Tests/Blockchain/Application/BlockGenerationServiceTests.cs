using AElf.Kernel.Blockchain.Infrastructure;

namespace AElf.Kernel.Blockchain.Application;

[Trait("Category", AElfMinerModule)]
public sealed class BlockGenerationServiceTests : AElfMinerTestBase
{
    private readonly BlockGenerationService _blockGenerationService;


    private readonly IStaticChainInformationProvider _staticChainInformationProvider;

    public BlockGenerationServiceTests()
    {
        _blockGenerationService = GetRequiredService<BlockGenerationService>();
        _staticChainInformationProvider = GetRequiredService<IStaticChainInformationProvider>();
    }

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
        block.Header.Time.ShouldBe(generateBlockDto.BlockTime);
        block.Header.ExtraData.Count.ShouldBe(1);
    }
}