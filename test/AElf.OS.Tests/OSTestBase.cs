using AElf.OS.Account;
using AElf.OS.BlockSync;
using AElf.OS.Handlers;
using AElf.OS.Worker;
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
    
    public class AccountServiceTestBase : AElfIntegratedTest<AccountServiceTestAElfModule>
    {
        
    }

    public class KeyStoreTestBase : AElfIntegratedTest<KeyStoreTestAElfModule>
    {
        
    }

    public class BlockSyncManyJobsTestBase : AElfIntegratedTest<BlockDownloadWorkerTestAElfModule>
    {
        
    }

    public class BlockSyncAbnormalPeerTestBase : AElfIntegratedTest<BlockSyncAbnormalPeerTestAElfModule>
    {
    }
    
    public class BlockSyncAttachBlockAbnormalPeerTestBase : AElfIntegratedTest<BlockSyncAttachBlockAbnormalPeerTestAElfModule>
    {
    }
    
    public class BlockSyncRetryTestBase : AElfIntegratedTest<BlockSyncRetryTestAElfModule>
    {
    }
    
    public class PeerDiscoveryWorkerTestBase : AElfIntegratedTest<PeerDiscoveryWorkerTestModule>
    {
    }
    
    public class PeerReconnectionTestBase : AElfIntegratedTest<PeerReconnectionTestAElfModule>
    {
    }
    
    public class AbnormalPeerEventHandlerTestBase : AElfIntegratedTest<AbnormalPeerEventHandlerTestAElfModule>
    {
    }
}