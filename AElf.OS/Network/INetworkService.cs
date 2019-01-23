using System.Threading.Tasks;

namespace AElf.OS.Network
{
    public interface INetworkService
    {
        Task Start();
        Task Stop();
    }
}