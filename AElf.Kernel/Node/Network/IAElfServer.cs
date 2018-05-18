using System.Threading;
using System.Threading.Tasks;

namespace AElf.Kernel.Node.Network
{
    public interface IAElfServer
    {
        Task Start(CancellationToken? token = null);
    }
}