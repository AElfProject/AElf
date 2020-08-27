using AElf.Modularity;
using AElf.OS.Network;
using Volo.Abp.Modularity;

namespace AElf.OS.Handlers
{
    [DependsOn(
        typeof(NetworkServiceTestModule),
        typeof(OSTestAElfModule))]
    public class AbnormalPeerEventHandlerTestAElfModule : AElfModule
    {

    }
    
    [DependsOn(
        typeof(NetworkServicePropagationTestModule),
        typeof(OSTestAElfModule))]
    public class NetworkBroadcastTestAElfModule : AElfModule
    {

    }
    
    [DependsOn(
        typeof(PeerInvalidTransactionTestModule),
        typeof(OSTestAElfModule))]
    public class TransactionValidationFailedEventHandlerTestAElfModule : AElfModule
    {

    }
}