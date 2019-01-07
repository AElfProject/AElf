using AElf.Database;
using AElf.Kernel.Storages;
using Volo.Abp;

namespace AElf.Contracts.Authorization.Tests
{
    public class ContractTestBase : TestBase.AElfIntegratedTest<ContractTestAElfModule>
    {
        protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
        {
            base.SetAbpApplicationCreationOptions(options);

            options.Services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());
            options.Services.AddKeyValueDbContext<BlockChainKeyValueDbContext>(o => o.UseInMemoryDatabase());        }
    }
}