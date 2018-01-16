using System.Threading.Tasks;

namespace AElf.Kernel
{
    public class ChainManager : IChainManager
    {
        public Task AddBlockAsync(IChain chain, IBlock block)
        {
            throw new System.NotImplementedException();
        }

        private bool CheckBlock(IBlock block)
        {
            return true;
        }
    }
}