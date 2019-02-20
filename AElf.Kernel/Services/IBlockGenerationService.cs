using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Services
{

    public class GenerateBlockDto
    {
        public int ChainId { get; set; }
        public Hash PreviousBlockHash { get; set; }
        public ulong PreviousBlockHeight { get; set; }
        
    }
    
    public interface IBlockGenerationService
    {
        Block GenerateBlockAsync(GenerateBlockDto generateBlockDto);
        Task FillBlockAsync(Block block, HashSet<TransactionResult> results);
        
    }
}