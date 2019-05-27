using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.Blockchain.Application
{
    public interface IBlockGenerationService
    {
        Task<Block> GenerateBlockBeforeExecutionAsync(GenerateBlockDto generateBlockDto);

        Task<Block> FillBlockAfterExecutionAsync(BlockHeader blockHeader, List<Transaction> transactions,
            List<ExecutionReturnSet> blockExecutionReturnSet);
    }
}