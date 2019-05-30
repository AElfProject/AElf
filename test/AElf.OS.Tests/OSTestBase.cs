using AElf.TestBase;

namespace AElf.OS
{
    // ReSharper disable once InconsistentNaming
    public class OSTestBase : AElfIntegratedTest<OSTestAElfModule>
    {
        
    }
    
    public class SyncNotForkedTestBase : AElfIntegratedTest<SyncNotForkedTestAElfModule>
    {
        
    }
    
    public class SyncTestBase : AElfIntegratedTest<SyncTestModule>
    {
        
    }

    public class SyncForkedTestBase : AElfIntegratedTest<SyncForkedTestAElfModule>
    {
        
    }
}