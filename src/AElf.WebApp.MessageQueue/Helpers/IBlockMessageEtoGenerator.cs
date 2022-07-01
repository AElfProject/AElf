using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;

namespace AElf.WebApp.MessageQueue.Helpers;

public interface IBlockMessageEtoGenerator
{
    Task<IBlockMessage> GetBlockMessageEtoByHeightAsync(long height, CancellationToken cts);
    IBlockMessage GetBlockMessageEto(BlockExecutedSet blockExecutedSet);
}