using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Blockchain.Application
{

    public class GenerateBlockDto
    {
        public int ChainId { get; set; }
        public Hash PreviousBlockHash { get; set; }
        public ulong PreviousBlockHeight { get; set; }
        
    }
    
    public interface IBlockGenerationService
    {
        Task<Block> GenerateBlockAsync(GenerateBlockDto generateBlockDto);
        
    }
}