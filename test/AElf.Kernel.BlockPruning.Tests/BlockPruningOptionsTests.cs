using Microsoft.Extensions.Options;
using Volo.Abp.Modularity;

namespace AElf.Kernel.BlockPruning;

public sealed class BlockPruningOptionsTests : BlockPruningTestBase
{
    private readonly BlockPruningOptions _options;

    public BlockPruningOptionsTests()
    {
        _options = GetRequiredService<IOptionsSnapshot<BlockPruningOptions>>().Value;
    }

    [Fact]
    public void RetainDistance_Default_ShouldBeAtLeastMinRetainDistance()
    {
        _options.RetainDistance.ShouldBeGreaterThanOrEqualTo(BlockPruningConstants.MinRetainDistance);
    }

    [Fact]
    public void BatchSize_ShouldBeAtLeast1()
    {
        _options.BatchSize.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void PruneThreshold_ShouldBeAtLeast0()
    {
        _options.PruneThreshold.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void BatchDelayMilliseconds_ShouldBeAtLeast0()
    {
        _options.BatchDelayMilliseconds.ShouldBeGreaterThanOrEqualTo(0);
    }
}

public sealed class BlockPruningOptionsCorrectionTests : BlockPruningConfigCorrectionTestBase
{
    private readonly BlockPruningOptions _options;

    public BlockPruningOptionsCorrectionTests()
    {
        _options = GetRequiredService<IOptionsSnapshot<BlockPruningOptions>>().Value;
    }

    [Fact]
    public void RetainDistance_BelowMin_ShouldBeCorrectedToMin()
    {
        _options.RetainDistance.ShouldBe(BlockPruningConstants.MinRetainDistance);
    }

    [Fact]
    public void BatchSize_Zero_ShouldBeCorrectedTo1()
    {
        _options.BatchSize.ShouldBe(1);
    }

    [Fact]
    public void PruneThreshold_Negative_ShouldBeCorrectedTo0()
    {
        _options.PruneThreshold.ShouldBe(0);
    }

    [Fact]
    public void BatchDelayMilliseconds_Negative_ShouldBeCorrectedTo0()
    {
        _options.BatchDelayMilliseconds.ShouldBe(0);
    }
}

public sealed class BlockPruningBatchSizeUpperLimitTests : BlockPruningBatchSizeUpperLimitTestBase
{
    private readonly BlockPruningOptions _options;

    public BlockPruningBatchSizeUpperLimitTests()
    {
        _options = GetRequiredService<IOptionsSnapshot<BlockPruningOptions>>().Value;
    }

    [Fact]
    public void BatchSize_AboveMax_ShouldBeClampedToMax()
    {
        _options.BatchSize.ShouldBe(BlockPruningConstants.MaxBatchSize);
    }
}
