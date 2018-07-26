using System.Net.Sockets;
using System.Threading.Tasks;

namespace AElf.Network.Peers
{
    public interface INodeDialer
    {
        Task<TcpClient> DialAsync(int timeout);
    }
}