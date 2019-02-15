using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Services
{
    public interface IBlockExecutingService
    {
        Task ExecuteBlockAsync(int chainId, Hash blockHash);
    }
}