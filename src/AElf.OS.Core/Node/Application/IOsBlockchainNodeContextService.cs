using System.Threading.Tasks;
using AElf.OS.Node.Domain;

namespace AElf.OS.Node.Application
{
    public interface IOsBlockchainNodeContextService
    {
        Task<OsBlockchainNodeContext> StartAsync(OsBlockchainNodeContextStartDto dto);

        Task StopAsync(OsBlockchainNodeContext blockchainNodeContext);
    }
}