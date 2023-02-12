using AElf.Kernel.Node.Domain;
using AElf.OS.Network.Infrastructure;

namespace AElf.OS.Node.Domain;

public class OsBlockchainNodeContext
{
    public BlockchainNodeContext BlockchainNodeContext { get; set; }

    public IAElfNetworkServer AElfNetworkServer { get; set; }
}