using AElf.Database;
using Volo.Abp;

namespace AElf.Miner.Tests
{
    public class MinerTestBase : AElf.TestBase.AElfIntegratedTest<MinerTestAElfModule>
    {
        protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
        {
            base.SetAbpApplicationCreationOptions(options);
            options.UseInMemoryDatabase();
        }
    }
}