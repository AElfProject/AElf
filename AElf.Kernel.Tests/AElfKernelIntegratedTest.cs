using AElf.Database;
using AElf.TestBase;
using Volo.Abp;

namespace AElf.Kernel.Tests
{
    public class AElfKernelIntegratedTest : AElfIntegratedTest<KernelTestAElfModule>
    {
        public new T GetRequiredService<T>()
        {
            return base.GetRequiredService<T>();
        }

        protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
        {
            
            //config test project to use in memory database
            options.UseInMemoryDatabase();
            options.UseAutofac();
        }
    }
}