using System.Threading.Tasks;

namespace AElf.OS.Network.Infrastructure
{
    public interface IAElfNetworkServer 
    {
        IPeerPool PeerPool { get; }
    }
}