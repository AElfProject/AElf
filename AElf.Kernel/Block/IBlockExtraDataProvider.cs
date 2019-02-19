using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IBlockExtraDataProvider
    {
        Task FillExtraDataAsync(Block block);
        Task<bool> ValidateExtraDataAsync(Block block);
    }
}