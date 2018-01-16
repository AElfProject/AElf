using System.Threading.Tasks;

namespace AElf.Kernel
{
    public class ChainManager : IChainManager
    {
        public Task AddBlockAsync(IChain chain, IBlock block)
        {
            if (CheckBlock(block))
            {
                chain.add
            }
        }

        private bool CheckBlock(IBlock block)
        {
            return true;
        }
    }
}