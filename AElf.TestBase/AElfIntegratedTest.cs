using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.TestBase
{
    public class AElfIntegratedTest<TModule> : AbpIntegratedTest<TModule>
        where TModule: IAbpModule
    {
        protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
        {
            options.UseAutofac();
        }
    }

    public class AElfIntegratedTest : AElfIntegratedTest<TestBaseAElfModule>
    {
        
    }
}