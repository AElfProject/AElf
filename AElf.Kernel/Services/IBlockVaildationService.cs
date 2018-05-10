using System.Threading.Tasks;

namespace AElf.Kernel.Services
{
    public interface IBlockVaildationService
    {
        Task<bool> ValidateBlockAsync(Block block, IChainContext context);
    }
}