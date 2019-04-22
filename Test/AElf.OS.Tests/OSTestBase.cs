using AElf.TestBase;
using Volo.Abp.Modularity;

namespace AElf.OS
{
    // ReSharper disable once InconsistentNaming
    public class OSTestBase : AElfIntegratedTest<OSTestAElfModule>
    {
        
    }
    
    public class NetWorkTestBase : AElfIntegratedTest<NetTestAElfModule>
    {
        
    }
    
    public class SyncTestBase : AElfIntegratedTest<SyncTestModule>
    {
        
    }
}