using System.Threading.Tasks;

namespace AElf.OS.Network.Grpc
{
    public interface IConnectionInfoProvider
    {
        Task<ConnectionInfo> GetConnectionInfoAsync();
    }
}