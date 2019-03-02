using System.Threading.Tasks;

namespace AElf.Kernel.Blockchain.Application
{
    public interface IBlockExtraDataProvider
    {
        Task FillExtraDataAsync(Block block);
        Task<bool> ValidateExtraDataAsync(Block block);
    }
}