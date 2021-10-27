using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.Kernel.SmartContractExecution;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Miner.Application
{
    public interface IMiningService
    {
        /// <summary>
        /// This method mines a block.
        /// </summary>
        /// <returns>The block that has been produced.</returns>
        Task<BlockExecutedSet> MineAsync(RequestMiningDto requestMiningDto, List<Transaction> transactions, Timestamp blockTime);
    }
}