using AElf.TestBase;

namespace AElf.Kernel
{
    public class AElfKernelTestBase : AElfIntegratedTest<KernelCoreTestAElfModule>
    {
    }
    
    public class AElfMinerTestBase : AElfIntegratedTest<KernelMinerTestAElfModule>
    {
    }
    
    public class AElfKernelCreateChainTestBase : AElfIntegratedTest<KernelCoreCreateChainTestModule>
    {
    }
}