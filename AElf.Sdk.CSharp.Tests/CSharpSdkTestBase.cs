using AElf.Database;
using Volo.Abp;

namespace AElf.Sdk.CSharp.Tests
{
    public class CSharpSdkTestBase : AElf.TestBase.AElfIntegratedTest<CSharpSdkAElfModule>
    {
        protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
        {
            base.SetAbpApplicationCreationOptions(options);
            options.UseInMemoryDatabase();
        }
    }
}