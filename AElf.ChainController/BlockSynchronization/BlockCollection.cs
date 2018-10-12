using System.Threading.Tasks;
using AElf.Kernel;

// ReSharper disable once CheckNamespace
namespace AElf.ChainController
{
    public class BlockCollection : IBlockCollection
    {
        public static BlockCollection Instance { get; } = new BlockCollection();

        private readonly PendingBlocks _pendingBlocks = new PendingBlocks();

        private BlockCollection()
        {
        }

        public async Task AddBlock(IBlock block)
        {
            await _pendingBlocks.AddBlock(block);
        }
    }
}