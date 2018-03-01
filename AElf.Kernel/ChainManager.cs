using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public class ChainManager : IChainManager
    {
        /// <summary>
        /// Adds the block async, permanent storage is required
        /// </summary>
        /// <returns>The block async.</returns>
        /// <param name="chain">Chain.</param>
        /// <param name="block">Block.</param>
        public Task AddBlockAsync(IChain chain, IBlock block)
        {
            return new Task(() =>(chain as Chain)?.Blocks.Add(block as Block));
        }                                

    }
}