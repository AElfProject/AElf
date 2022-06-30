using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;

namespace AElf.WebApp.MessageQueue.Helpers;

public interface IBlockMessageEtoGenerator
{
    Task<object> GetBlockMessageEtoByHeightAsync(long height, CancellationToken cts);
    object GetBlockMessageEto(BlockExecutedSet blockExecutedSet);
}