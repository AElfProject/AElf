using AElf.OS.Network;
using AElf.TestBase;

namespace AElf.OS
{
    public class OSCoreNetworkServiceTestBase : AElfIntegratedTest<NetworkServiceTestModule>
    {
    
    }

    public class NetworkServicePropagationTestBase : AElfIntegratedTest<NetworkServicePropagationTestModule>
    {
        
    }

    public class SyncFlagTestBase : AElfIntegratedTest<OSCoreSyncFlagTestModule>
    {
        
    }

    public class HandshakeTestBase : AElfIntegratedTest<OSCoreHandshakeTestModule>
    {
        
    }

    public class PeerInvalidTransactionTestBase : AElfIntegratedTest<PeerInvalidTransactionTestModule>
    {
        
    }
    
    public class PeerDiscoveryTestBase : AElfIntegratedTest<PeerDiscoveryTestModule>
    {
        
    }
}