using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.OS.Network.Grpc
{
    public interface IAElfNetworkServer
    {
        Task StartAsync();
        Task StopAsync();
    }
}