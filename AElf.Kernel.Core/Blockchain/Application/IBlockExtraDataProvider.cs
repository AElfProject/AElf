using System.Threading.Tasks;

namespace AElf.Kernel.Blockchain.Application
{
    public interface IBlockExtraDataProvider
    {
        Task FillExtraDataAsync(int chainId, Block block);
        Task<bool> ValidateExtraDataAsync(int chainId, Block block);
    }
}