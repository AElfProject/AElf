using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Blockchain.Application
{
    public interface IBlockExecutingService
    {
        Task ExecuteBlockAsync(int chainId, Hash blockHash);
    }
    
    public class BlockExecutingService : IBlockExecutingService
    {
        public async Task ExecuteBlockAsync(int chainId, Hash blockHash)
        {
            throw new System.NotImplementedException();
        }
    }
}