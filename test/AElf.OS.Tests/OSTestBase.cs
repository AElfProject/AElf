using AElf.OS.BlockSync;
using AElf.TestBase;
using Volo.Abp.Modularity;

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
    
    public class BlockSyncTestBase : AElfIntegratedTest<BlockSyncTestAElfModule>
    {
        
    }

    public class BlockSyncForkedTestBase : AElfIntegratedTest<BlockSyncForkedTestAElfModule>
    {
        
    }
}