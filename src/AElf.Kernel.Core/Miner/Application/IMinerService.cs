using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using AElf.Types;

namespace AElf.Kernel.Miner.Application
{
    public interface IMiningService
    {
        /// <summary>
        /// This method mines a block.
        /// </summary>
        /// <returns>The block that has been produced.</returns>
        Task<Block> MineAsync(Hash previousBlockHash, long previousBlockHeight, List<Transaction> transactions,
            Timestamp blockTime, TimeSpan blockExecutionTime);
    }
}