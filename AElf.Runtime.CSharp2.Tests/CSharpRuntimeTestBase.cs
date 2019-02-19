using AElf.Database;
using Volo.Abp;

namespace AElf.Runtime.CSharp2.Tests
{
    public class CSharpRuntimeTestBase : AElf.TestBase.AElfIntegratedTest<TestCSharpRuntimeAElfModule>
    {
        protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
        {
            base.SetAbpApplicationCreationOptions(options);

        }
        
        
    }
}