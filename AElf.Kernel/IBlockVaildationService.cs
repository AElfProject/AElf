using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IBlockVaildationService
    {
        Task<bool> ValidateBlockAsync(Block block, IChainContext context);
    }
}