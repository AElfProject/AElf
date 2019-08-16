using System.Threading.Tasks;

namespace AElf.OS.Network.Application
{
    public interface IPeerDiscoveryService
    {
        Task<NodeList> DiscoverNodesAsync();
        Task<NodeList> GetNodesAsync(int maxCount);
        Task AddNodeAsync(NodeInfo nodeInfo);
    }
}