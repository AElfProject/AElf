using AElf.TestBase;

namespace AElf.Kernel.Tests
{
    public class AElfKernelIntegratedTest : AElfIntegratedTest<KernelTestAElfModule>
    {
        public new T GetRequiredService<T>()
        {
            return base.GetRequiredService<T>();
        }
    }
}