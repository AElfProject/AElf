using System.Collections.Generic;
using AElf.Kernel.Blockchain;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Miner.Application;

public interface IMiningService
{
    /// <summary>
    ///     This method mines a block.
    /// </summary>
    /// <returns>The block that has been produced.</returns>
    Task<BlockExecutedSet> MineAsync(RequestMiningDto requestMiningDto, List<Transaction> transactions,
        Timestamp blockTime);
}