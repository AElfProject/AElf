using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Services
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