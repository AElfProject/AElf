using System.Threading.Tasks;

namespace AElf.Kernel.BlockPruning.Domain;

public interface IBlockPruningInfoManager
{
    Task<long> GetLastPrunedHeightAsync();
    Task SetLastPrunedHeightAsync(long height);
}


