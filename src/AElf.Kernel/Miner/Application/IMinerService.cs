using System.Threading.Tasks;
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
        Task<Block> MineAsync(Hash previousBlockHash, long previousBlockHeight, Timestamp blockTime,
            Duration blockExecutionTime);
    }
}