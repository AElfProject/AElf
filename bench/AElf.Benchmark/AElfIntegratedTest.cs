using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Testing;
using Volo.Abp.Autofac;

namespace AElf.Benchmark
{
    public class AElfIntegratedTest<TModule> : AbpIntegratedTest<TModule>
        where TModule : IAbpModule
    {
        protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
        {
            options.UseAutofac();
        }
    }
}