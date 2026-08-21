using System;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Kernel.BlockPruning;

[DependsOn(
    typeof(CoreKernelAElfModule)
)]
public class BlockPruningAElfModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        Configure<BlockPruningOptions>(configuration.GetSection("BlockPruning"));
        context.Services.PostConfigure<BlockPruningOptions>(options =>
        {
            options.RetainDistance = Math.Max(options.RetainDistance, BlockPruningConstants.MinRetainDistance);
            options.BatchSize = Math.Clamp(options.BatchSize, 1, BlockPruningConstants.MaxBatchSize);
            options.PruneThreshold = Math.Max(options.PruneThreshold, 0);
            options.BatchDelayMilliseconds = Math.Max(options.BatchDelayMilliseconds, 0);
        });

        context.Services.AddStoreKeyPrefixProvide<BlockPruningInfo>("bp");
    }

    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
        var taskQueueManager = context.ServiceProvider.GetRequiredService<ITaskQueueManager>();
        taskQueueManager.CreateQueue(BlockPruningConstants.BlockPruningQueueName);
    }
}
