using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.Blockchain.Application
{
    public class GenerateBlockDto
    {
        public Hash PreviousBlockHash { get; set; }
        public long PreviousBlockHeight { get; set; }

        public DateTime BlockTime { get; set; } = DateTime.UtcNow;
    }

    public interface IBlockGenerationService
    {
        Task<Block> GenerateBlockBeforeExecutionAsync(GenerateBlockDto generateBlockDto);

        Task<Block> FillBlockAfterExecutionAsync(BlockHeader blockHeader, List<Transaction> transactions,
            List<ExecutionReturnSet> blockExecutionReturnSet);
    }
}