using AElf.Database;
using AElf.Modularity;
using Volo.Abp;

namespace AElf.TestBase.Contract
{
    public class ContractTestBase<TModule> : TestBase.AElfIntegratedTest<TModule>
        where TModule:AElfModule
    {
        protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
        {
            base.SetAbpApplicationCreationOptions(options);
            options.UseInMemoryDatabase();
        }
    }
}