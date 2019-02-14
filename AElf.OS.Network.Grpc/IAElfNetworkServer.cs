using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.OS.Network.Grpc
{
    public interface IAElfNetworkServer
    {
        Task<bool> AddPeerAsync(string address);
        Task<bool> RemovePeerAsync(string address);
        List<GrpcPeer> GetPeers();
        
        Task StartAsync();
        Task StopAsync();
    }
}