using AElf.Database;
using AElf.Kernel.Storages;
using Volo.Abp;

namespace AElf.Miner.Tests
{
    public class MinerTestBase : AElf.TestBase.AElfIntegratedTest<MinerTestAElfModule>
    {
        protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
        {
            base.SetAbpApplicationCreationOptions(options);

            options.Services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());
            options.Services.AddKeyValueDbContext<BlockChainKeyValueDbContext>(o => o.UseInMemoryDatabase());        }
    }
}