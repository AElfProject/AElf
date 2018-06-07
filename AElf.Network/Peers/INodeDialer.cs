using System.Threading.Tasks;
using AElf.Network.Data;

namespace AElf.Network.Peers
{
    public interface INodeDialer
    {
        Task<IPeer> DialAsync(NodeData distantNode);
    }
}