using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Blk
{
    public interface IBlockGenerationService
    {
        Task<Block> GenerateBlockAsync(GenerateBlockDto generateBlockDto);
        void FillBlockAsync(Block block, HashSet<TransactionResult> results);
    }

    public class GenerateBlockDto
    {
        public int ChainId { get; set; }
        public Hash PreBlockHash { get; set; }
        public ulong PreBlockHeight { get; set; }
    }
}