using System.Threading.Tasks;

namespace AElf.Node
{
    public interface INetworkService
    {
        Task Start();
        Task Stop();
    }
}