using AElf.Database;
using AElf.TestBase;
using Volo.Abp;

namespace AElf.Kernel.Tests
{
    public class AElfKernelTestBase : AElfIntegratedTest<KernelTestAElfModule>
    {
        public new T GetRequiredService<T>()
        {
            return base.GetRequiredService<T>();
        }
    }
}