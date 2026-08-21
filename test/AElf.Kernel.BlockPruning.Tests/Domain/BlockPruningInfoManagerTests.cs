using AElf.Kernel.BlockPruning.Domain;

namespace AElf.Kernel.BlockPruning;

public sealed class BlockPruningInfoManagerTests : BlockPruningTestBase
{
    private readonly IBlockPruningInfoManager _blockPruningInfoManager;

    public BlockPruningInfoManagerTests()
    {
        _blockPruningInfoManager = GetRequiredService<IBlockPruningInfoManager>();
    }

    [Fact]
    public async Task GetLastPrunedHeight_Initial_ShouldReturn0()
    {
        var height = await _blockPruningInfoManager.GetLastPrunedHeightAsync();
        height.ShouldBe(0);
    }

    [Fact]
    public async Task SetAndGet_ShouldReturnCorrectValue()
    {
        await _blockPruningInfoManager.SetLastPrunedHeightAsync(1000);
        var height = await _blockPruningInfoManager.GetLastPrunedHeightAsync();
        height.ShouldBe(1000);
    }

    [Fact]
    public async Task MultipleUpdates_ShouldReturnLatest()
    {
        await _blockPruningInfoManager.SetLastPrunedHeightAsync(100);
        await _blockPruningInfoManager.SetLastPrunedHeightAsync(200);
        await _blockPruningInfoManager.SetLastPrunedHeightAsync(300);

        var height = await _blockPruningInfoManager.GetLastPrunedHeightAsync();
        height.ShouldBe(300);
    }
}
