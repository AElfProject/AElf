using System.Threading.Tasks;

namespace AElf.Kernel.Blockchain.Application
{
    public interface IBlockExtraDataProvider
    {
        Task FillExtraData(int chainId, Block block);
        Task<bool> ValidateExtraData(int chainId, Block block);
    }
}