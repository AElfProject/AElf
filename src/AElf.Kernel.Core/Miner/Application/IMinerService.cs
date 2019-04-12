using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Miner.Application
{
    public interface IMinerService
    {
        /// <summary>
        /// This method mines a block.
        /// </summary>
        /// <returns>The block that has been produced.</returns>
        Task<Block> MineAsync(Hash previousBlockHash, long previousBlockHeight, DateTime blockTime, TimeSpan timeSpan);
    }

    public interface IMiningService
    {
        /// <summary>
        /// This method mines a block.
        /// </summary>
        /// <returns>The block that has been produced.</returns>
        Task<Block> MineAsync(Hash previousBlockHash, long previousBlockHeight, List<Transaction> transactions,
            DateTime blockTime, TimeSpan timeSpan);
    }
}