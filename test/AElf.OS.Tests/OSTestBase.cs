using AElf.OS.BlockSync;
using AElf.TestBase;

namespace AElf.OS
{
    // ReSharper disable once InconsistentNaming
    public class OSTestBase : AElfIntegratedTest<OSTestAElfModule>
    {
        
    }

    public class BlockSyncTestBase : AElfIntegratedTest<BlockSyncTestAElfModule>
    {
        
    }

    public class BlockSyncForkedTestBase : AElfIntegratedTest<BlockSyncForkedTestAElfModule>
    {
        
    }

    public class KeyStoreTestBase : AElfIntegratedTest<KeyStoreTestAElfModule>
    {
        
    }

    public class BlockSyncManyJobsTestBase : AElfIntegratedTest<BlockDownloadWorkerTestAElfModule>
    {
        
    }
}