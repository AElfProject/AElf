using System.Threading.Tasks;
namespace AElf.Kernel.Blk
{
    public interface IBlockExtraDataProvider
    {
        Task FillExtraData(Block block);
        Task<bool> ValidateExtraData(Block block);
    }
}