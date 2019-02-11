using System.Threading.Tasks;

namespace AElf.Kernel.BlockService
{
    public interface IBlockExtraDataProvider
    {
        Task TryAddExtraData(Block block);
    }
}