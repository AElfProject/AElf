using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

// ReSharper disable once CheckNamespace
namespace AElf.ChainController
{
    // ReSharper disable InconsistentNaming
    public interface IBlockExecutionService
    {
        Task<BlockExecutionResult> ExecuteBlock(IBlock block);
        void Init();
        Task Rollback(List<Transaction> txs);
    }
}