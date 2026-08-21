using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.Kernel.BlockPruning;

[DependsOn(
    typeof(AbpEventBusModule),
    typeof(BlockPruningAElfModule),
    typeof(TestBaseKernelAElfModule))]
public class BlockPruningTestModule : AElfModule
{
    // public override void ConfigureServices(ServiceConfigurationContext context)
    // {
    //     context.Services.AddSingleton<KernelTestHelper>();
    // }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var kernelTestHelper = context.ServiceProvider.GetService<KernelTestHelper>();
        AsyncHelper.RunSync(() => kernelTestHelper!.MockChainAsync());
    }
}

/// <summary>
/// LIB=5, RetainDistance=2 -> pruneTarget=3, prunable range: heights 2~3
/// </summary>
[DependsOn(typeof(BlockPruningTestModule))]
public class BlockPruningServiceTestModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.PostConfigure<BlockPruningOptions>(options =>
        {
            options.Enabled = true;
            options.RetainDistance = 2;
            options.BatchSize = 100;
            options.PruneThreshold = 0;
            options.BatchDelayMilliseconds = 0;
        });
    }
}

[DependsOn(typeof(BlockPruningTestModule))]
public class BlockPruningDisabledTestModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.PostConfigure<BlockPruningOptions>(options =>
        {
            options.Enabled = false;
            options.RetainDistance = 2;
            options.BatchSize = 100;
            options.PruneThreshold = 0;
            options.BatchDelayMilliseconds = 0;
        });
    }
}

/// <summary>
/// LIB=5, RetainDistance=0 -> pruneTarget=5, prunable range: heights 2~5. BatchSize=2.
/// </summary>
[DependsOn(typeof(BlockPruningTestModule))]
public class BlockPruningBatchTestModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.PostConfigure<BlockPruningOptions>(options =>
        {
            options.Enabled = true;
            options.RetainDistance = 0;
            options.BatchSize = 2;
            options.PruneThreshold = 0;
            options.BatchDelayMilliseconds = 0;
        });
    }
}

/// <summary>
/// LIB=5, RetainDistance=2, PruneThreshold=100
///   => pruneTarget = 5 - 2 = 3, gap = 3 - 0 = 3 &lt; 100 => pruning skipped
/// </summary>
[DependsOn(typeof(BlockPruningTestModule))]
public class BlockPruningThresholdTestModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.PostConfigure<BlockPruningOptions>(options =>
        {
            options.Enabled = true;
            options.RetainDistance = 2;
            options.BatchSize = 100;
            options.PruneThreshold = 100;
            options.BatchDelayMilliseconds = 0;
        });
    }
}

/// <summary>
/// Verify PostConfigure corrects invalid values.
/// BlockPruningConfigCorrectionTestModule sets below-minimum values to validate correction.
/// </summary>
[DependsOn(typeof(BlockPruningTestModule))]
public class BlockPruningConfigCorrectionTestModule : AElf.Modularity.AElfModule
{
    public override void ConfigureServices(Volo.Abp.Modularity.ServiceConfigurationContext context)
    {
        Configure<BlockPruningOptions>(options =>
        {
            options.RetainDistance = 100;
            options.BatchSize = 0;
            options.PruneThreshold = -1;
            options.BatchDelayMilliseconds = -1;
        });
    }
}

[DependsOn(typeof(BlockPruningTestModule))]
public class BlockPruningBatchSizeUpperLimitTestModule : AElf.Modularity.AElfModule
{
    public override void ConfigureServices(Volo.Abp.Modularity.ServiceConfigurationContext context)
    {
        Configure<BlockPruningOptions>(options =>
        {
            options.BatchSize = 999999;
        });
    }
}
