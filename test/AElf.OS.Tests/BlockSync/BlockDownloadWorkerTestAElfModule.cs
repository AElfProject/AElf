using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.OS.BlockSync
{
    [DependsOn(typeof(BlockSyncTestAElfModule))]
    public class BlockDownloadWorkerTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<BlockSyncOptions>(o =>
            {
                o.MaxBlockDownloadCount = 3;
                o.MaxBatchRequestBlockCount = 3;
            });
        }
    }
}