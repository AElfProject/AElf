using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.BlockService
{
    public interface IBlockGenerationService
    {
        Task<IBlock> GenerateBlockAsync(HashSet<TransactionResult> results, DateTime currentBlockTime);
    }
}