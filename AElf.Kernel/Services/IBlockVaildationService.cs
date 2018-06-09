using System.Threading.Tasks;

namespace AElf.Kernel.Services
{
    public interface IBlockVaildationService
    {
        Task<bool> ValidateBlockAsync(IBlock block, IChainContext context);
    }
}