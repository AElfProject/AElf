using System.Threading.Tasks;

namespace AElf.Kernel.Blockchain.Application
{
    public interface IBlockExtraDataProvider
    {
        Task FillExtraData(Block block);
        Task<bool> ValidateExtraData(Block block);
    }
}