using System.Threading.Tasks;

namespace AElf.Kernel.BlockPruning.Application;

public interface IBlockPruningService
{
    Task PruneBlockchainDataAsync();
}
