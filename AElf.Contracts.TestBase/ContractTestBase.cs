using AElf.Database;
using AElf.Kernel.Storages;
using AElf.Modularity;
using AElf.TestBase;
using Volo.Abp;

namespace AElf.Contracts.TestBase
{
    public class ContractTestBase<TModule> : AElfIntegratedTest<TModule>
        where TModule:AElfModule
    {
        protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
        {
            base.SetAbpApplicationCreationOptions(options);

            options.Services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());
            options.Services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());        }
    }
}