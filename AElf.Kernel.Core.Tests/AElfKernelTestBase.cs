using AElf.TestBase;

namespace AElf.Kernel
{
    public class AElfKernelTestBase : AElfIntegratedTest<KernelCoreTestAElfModule>
    {
    }
    
    public class AElfKernelWithChainTestBase : AElfIntegratedTest<KernelCoreWithChainTestAElfModule>
    {
    }
    
    public class AElfMinerTestBase : AElfIntegratedTest<KernelMinerTestAElfModule>
    {
    }
}