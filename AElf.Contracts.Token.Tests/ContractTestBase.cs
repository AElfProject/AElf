using AElf.Database;
using Volo.Abp;

namespace AElf.Contracts.Token.Tests
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