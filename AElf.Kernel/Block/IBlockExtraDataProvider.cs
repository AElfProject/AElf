using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IBlockExtraDataProvider
    {
        Task FillExtraData(Block block);
        Task<bool> ValidateExtraData(Block block);
    }
}