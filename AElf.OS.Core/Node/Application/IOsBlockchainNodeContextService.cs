using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AElf.Kernel.Node.Application;
using AElf.Kernel.Node.Domain;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Node.Domain;

namespace AElf.OS.Node.Application
{
    public class OsBlockchainNodeContextStartDto
    {
        public BlockchainNodeContextStartDto BlockchainNodeContextStartDto { get; set; }
    }

    public interface IOsBlockchainNodeContextService
    {
        Task<OsBlockchainNodeContext> StartAsync(OsBlockchainNodeContextStartDto dto);

        Task StopAsync(OsBlockchainNodeContext blockchainNodeContext);
    }

    public class OsBlockchainNodeContextService : IOsBlockchainNodeContextService
    {
        private IBlockchainNodeContextService _blockchainNodeContextService;
        private IAElfNetworkServer _networkServer;

        public OsBlockchainNodeContextService(IBlockchainNodeContextService blockchainNodeContextService,
            IAElfNetworkServer networkServer)
        {
            _blockchainNodeContextService = blockchainNodeContextService;
            _networkServer = networkServer;
        }

        public async Task<OsBlockchainNodeContext> StartAsync(OsBlockchainNodeContextStartDto dto)
        {
            var context = new OsBlockchainNodeContext
            {
                BlockchainNodeContext =
                    await _blockchainNodeContextService.StartAsync(dto.BlockchainNodeContextStartDto)
            };
            context.AElfNetworkServer = _networkServer;
            
            return context;
        }

        public async Task StopAsync(OsBlockchainNodeContext blockchainNodeContext)
        {
            await _blockchainNodeContextService.StopAsync(blockchainNodeContext.BlockchainNodeContext);

        }
    }
}