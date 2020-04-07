using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface IBlockchainExecutingService
    {
        Task<BlockExecutionResult> ExecuteBlocksAsync(IEnumerable<Block> blocks);
    }
}