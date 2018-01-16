using System.Threading.Tasks;

namespace AElf.Kernel
{
    public class ChainManager : IChainManager
    {
        public Task AddBlockAsync(IChain chain, IBlock block)
        {
            throw new System.NotImplementedException();

            //TODO:
            //Check the block is valid for this chain.
            //Add the block to the chain.
        }

        private bool CheckBlock(IBlock block)
        {
            return true;
        }
    }
}