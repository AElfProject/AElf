using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Miner.Application
{
    public interface IBlockTemplateMinerService
    {
        Task<BlockHeader> CreateTemplateCacheAsync(Hash previousBlockHash, long previousBlockHeight,
            Timestamp blockTime,
            Duration blockExecutionTime);

        Task<Block> ChangeTemplateCacheBlockHeaderAndClearCacheAsync(BlockHeader blockHeader);
    }
}