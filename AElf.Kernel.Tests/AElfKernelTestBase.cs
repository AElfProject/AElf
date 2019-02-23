using AElf.TestBase;

namespace AElf.Kernel
{
    public class AElfKernelTestBase : AElfIntegratedTest<KernelTestAElfModule>
    {
        public new T GetRequiredService<T>()
        {
            return base.GetRequiredService<T>();
        }
    }
}