using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.Kernel.SmartContractExecution;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Miner.Application
{
    public interface IMinerService
    {
        /// <summary>
        /// This method mines a block.
        /// </summary>
        /// <returns>The block that has been produced.</returns>
        Task<BlockExecutedSet> MineAsync(Hash previousBlockHash, long previousBlockHeight, Timestamp blockTime,
            Duration blockExecutionTime);
    }
}