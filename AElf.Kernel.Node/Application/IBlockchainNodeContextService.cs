using System.Threading.Tasks;
using AElf.Kernel.Node.Domain;

namespace AElf.Kernel.Node.Application
{

    public class BlockchainNodeContextStartDto
    {
        public int ChainId { get; set; }
    }
    
    public interface IBlockchainNodeContextService
    {
        Task<BlockchainNodeContext> StartAsync(BlockchainNodeContextStartDto dto);
    }
    
    public class BlockchainNodeContextService: IBlockchainNodeContextService
    {
        public async Task<BlockchainNodeContext> StartAsync(BlockchainNodeContextStartDto dto)
        {
            throw new System.NotImplementedException();
        }
    }
}