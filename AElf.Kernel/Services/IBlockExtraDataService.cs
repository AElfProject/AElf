using System.Threading.Tasks;

namespace AElf.Kernel.Blk
{
    public interface IBlockExtraDataService
    {
        // todo: redefine needed especially return type, maybe a new structure ExtraData is needed.
        Task FillBlockExtraData(Block block);
    }
}