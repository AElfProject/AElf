using System.Threading.Tasks;
using AElf.Kernel.Node.Infrastructure;

namespace AElf.OS.Network.Infrastructure
{
    public interface IAElfNetworkServer : IChainRelatedComponent
    {
        IPeerPool PeerPool { get; }
    }
}