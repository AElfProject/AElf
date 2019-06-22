using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.OS.Network.Infrastructure;

namespace AElf.OS.Network.Application
{
    public interface IPeerDiscoveryService
    {
        Task<NodeList> DiscoverNodesAsync();
        Task<NodeList> GetNodesAsync(int maxCount);
    }
}