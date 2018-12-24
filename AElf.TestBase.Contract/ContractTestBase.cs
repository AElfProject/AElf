using AElf.Database;
using Volo.Abp;

namespace AElf.TestBase.Contract
{
    public class ContractTestBase : TestBase.AElfIntegratedTest<ContractTestAElfModule>
    {
        protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
        {
            base.SetAbpApplicationCreationOptions(options);
            options.UseInMemoryDatabase();
        }
    }
}