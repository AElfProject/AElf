using MartinCostello.Logging.XUnit;
using Volo.Abp;
using Volo.Abp.Modularity;
using Xunit.Abstractions;

namespace AElf.TestBase
{
    public class AElfIntegratedTest<TModule> : AbpIntegratedTest<TModule>
        where TModule : IAbpModule
    {
        protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
        {
            options.UseAutofac();
        }


        protected void SetTestOutputHelper(ITestOutputHelper testOutputHelper)
        {
            GetRequiredService<ITestOutputHelperAccessor>().OutputHelper = testOutputHelper;
        }
    }

    public class AElfIntegratedTest : AElfIntegratedTest<TestBaseAElfModule>
    {
    }
}