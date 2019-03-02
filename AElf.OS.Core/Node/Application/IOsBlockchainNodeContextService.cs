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
        private IChainRelatedComponentManager<IAElfNetworkServer> _networkServers;

        public OsBlockchainNodeContextService(IBlockchainNodeContextService blockchainNodeContextService,
            IChainRelatedComponentManager<IAElfNetworkServer> networkServers)
        {
            _blockchainNodeContextService = blockchainNodeContextService;
            _networkServers = networkServers;
        }

        public async Task<OsBlockchainNodeContext> StartAsync(OsBlockchainNodeContextStartDto dto)
        {
            var context = new OsBlockchainNodeContext
            {
                BlockchainNodeContext =
                    await _blockchainNodeContextService.StartAsync(dto.BlockchainNodeContextStartDto)
            };
            context.AElfNetworkServer = await _networkServers.CreateAsync(dto.BlockchainNodeContextStartDto.ChainId);
            return context;
        }

        public async Task StopAsync(OsBlockchainNodeContext blockchainNodeContext)
        {
            await _blockchainNodeContextService.StopAsync(blockchainNodeContext.BlockchainNodeContext);

            await _networkServers.RemoveAsync(blockchainNodeContext.BlockchainNodeContext.ChainId);
        }
    }
}