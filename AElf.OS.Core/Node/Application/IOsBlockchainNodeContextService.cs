using System.Threading.Tasks;
using AElf.Kernel.Node.Application;
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
    public class OsBlockchainNodeContextService: IOsBlockchainNodeContextService
    {

        private BlockchainNodeContextService _blockchainNodeContextService;

        public OsBlockchainNodeContextService(BlockchainNodeContextService blockchainNodeContextService)
        {
            _blockchainNodeContextService = blockchainNodeContextService;
        }

        public async Task<OsBlockchainNodeContext> StartAsync(OsBlockchainNodeContextStartDto dto)
        {
            await _blockchainNodeContextService.StartAsync(dto.BlockchainNodeContextStartDto);
            throw new System.NotImplementedException();
        }

        public async Task StopAsync(OsBlockchainNodeContext blockchainNodeContext)
        {
            throw new System.NotImplementedException();
        }
    }
}