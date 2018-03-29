using System.Threading;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IChainNode
    {
        /// <summary>
        /// Load an existing chain
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        Task LoadAsync(Hash chainId);
    }

    public class ChainNode : IChainNode
    {
        public Task LoadAsync(Hash chainId)
        {
            throw new System.NotImplementedException();
        }
    }
}